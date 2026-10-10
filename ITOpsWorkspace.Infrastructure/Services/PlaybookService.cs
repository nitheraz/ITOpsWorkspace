using Microsoft.EntityFrameworkCore;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Infrastructure.Data;

namespace ITOpsWorkspace.Infrastructure.Services;

public class PlaybookService : IPlaybookService
{
    private readonly string _dbPath;

    public PlaybookService(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            throw new ArgumentException(
                "A database path is required.",
                nameof(dbPath));
        }

        _dbPath = dbPath;
    }

    public async Task<List<Playbook>> GetAllPlaybooksAsync()
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        return await db.Playbooks
            .Include(p => p.Steps)
            .OrderBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<Playbook?> GetPlaybookAsync(int id)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        return await db.Playbooks
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SavePlaybookAsync(Playbook playbook)
    {
        ArgumentNullException.ThrowIfNull(playbook);

        if (string.IsNullOrWhiteSpace(playbook.Title))
        {
            throw new ArgumentException(
                "A playbook title is required.",
                nameof(playbook));
        }

        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        if (playbook.Id == 0)
        {
            var newPlaybook = new Playbook
            {
                Title = playbook.Title.Trim(),
                Description = playbook.Description?.Trim() ?? string.Empty,
                Steps = CreateOrderedSteps(playbook.Steps)
            };

            db.Playbooks.Add(newPlaybook);
        }
        else
        {
            var existing = await db.Playbooks
                .Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.Id == playbook.Id);

            if (existing is null)
            {
                throw new InvalidOperationException(
                    $"Playbook with ID {playbook.Id} was not found.");
            }

            existing.Title = playbook.Title.Trim();
            existing.Description =
                playbook.Description?.Trim() ?? string.Empty;

            // Run step states contain their own text snapshots.
            // Removing template steps does not remove run history.
            db.PlaybookSteps.RemoveRange(existing.Steps);

            existing.Steps = CreateOrderedSteps(playbook.Steps);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeletePlaybookAsync(int id)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        var playbook = await db.Playbooks
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (playbook is null)
        {
            return;
        }

        // Keep historical runs. PlaybookRun stores its title and ID,
        // while PlaybookRunStepState stores a snapshot of each step.
        db.PlaybookSteps.RemoveRange(playbook.Steps);
        db.Playbooks.Remove(playbook);

        await db.SaveChangesAsync();
    }

    public async Task<List<Playbook>> SearchPlaybooksAsync(
        string keywords)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        var query = db.Playbooks
            .Include(p => p.Steps)
            .AsQueryable();

        if (string.IsNullOrWhiteSpace(keywords))
        {
            return await query
                .OrderBy(p => p.Title)
                .ToListAsync();
        }

        var words = keywords
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (words.Count == 0)
        {
            return new List<Playbook>();
        }

        // Match at least one search term against the title,
        // description, or any step in the playbook.
        var all = await query.ToListAsync();

        return all
            .Where(playbook => words.Any(word =>
                playbook.Title.Contains(
                    word,
                    StringComparison.OrdinalIgnoreCase) ||
                playbook.Description.Contains(
                    word,
                    StringComparison.OrdinalIgnoreCase) ||
                playbook.Steps.Any(step =>
                    step.Text.Contains(
                        word,
                        StringComparison.OrdinalIgnoreCase))))
            .OrderBy(p => p.Title)
            .ToList();
    }

    public async Task<PlaybookRun> StartRunAsync(
        int playbookId,
        string incidentSysId,
        string incidentNumber)
    {
        if (string.IsNullOrWhiteSpace(incidentSysId))
        {
            throw new ArgumentException(
                "An incident ID is required.",
                nameof(incidentSysId));
        }

        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        // If this playbook is already active on this incident,
        // return that run rather than creating a duplicate.
        var existingRun = await db.PlaybookRuns
            .Include(r => r.StepStates)
            .Where(r =>
                r.IncidentSysId == incidentSysId &&
                r.PlaybookId == playbookId &&
                r.CompletedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();

        if (existingRun is not null)
        {
            return existingRun;
        }

        var playbook = await db.Playbooks
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Id == playbookId);

        if (playbook is null)
        {
            throw new InvalidOperationException(
                $"Playbook with ID {playbookId} was not found.");
        }

        var run = new PlaybookRun
        {
            PlaybookId = playbook.Id,
            PlaybookTitle = playbook.Title,
            IncidentSysId = incidentSysId,
            IncidentNumber = incidentNumber ?? string.Empty,
            StartedAt = DateTime.UtcNow,

            StepStates = playbook.Steps
                .OrderBy(s => s.Order)
                .Select(step => new PlaybookRunStepState
                {
                    PlaybookStepId = step.Id,
                    StepText = step.Text,
                    Order = step.Order,
                    IsChecked = false,
                    CheckedAt = null
                })
                .ToList()
        };

        db.PlaybookRuns.Add(run);

        await db.SaveChangesAsync();

        return run;
    }

    // Compatibility method: returns the most recently started active
    // run for an incident, regardless of which playbook it belongs to.
    public async Task<PlaybookRun?> GetActiveRunAsync(
        string incidentSysId)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        return await db.PlaybookRuns
            .Include(r => r.StepStates)
            .Where(r =>
                r.IncidentSysId == incidentSysId &&
                r.CompletedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<PlaybookRun>> GetActiveRunsAsync(
        string incidentSysId)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        return await db.PlaybookRuns
            .Include(r => r.StepStates)
            .Where(r =>
                r.IncidentSysId == incidentSysId &&
                r.CompletedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync();
    }

    public async Task<PlaybookRun?> GetActiveRunAsync(
        string incidentSysId,
        int playbookId)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        return await db.PlaybookRuns
            .Include(r => r.StepStates)
            .Where(r =>
                r.IncidentSysId == incidentSysId &&
                r.PlaybookId == playbookId &&
                r.CompletedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SetStepCheckedAsync(
        int runStepStateId,
        bool isChecked)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        var state = await db.PlaybookRunStepStates
            .FirstOrDefaultAsync(s => s.Id == runStepStateId);

        if (state is null)
        {
            throw new InvalidOperationException(
                $"Playbook run step with ID {runStepStateId} was not found.");
        }

        var runIsActive = await db.PlaybookRuns.AnyAsync(r =>
            r.Id == state.PlaybookRunId &&
            r.CompletedAt == null);

        if (!runIsActive)
        {
            throw new InvalidOperationException(
                "This playbook run has already been completed.");
        }

        state.IsChecked = isChecked;
        state.CheckedAt = isChecked ? DateTime.UtcNow : null;

        await db.SaveChangesAsync();
    }

    public async Task CompleteRunAsync(int runId)
    {
        using var db = new AppDbContext(_dbPath);

        await db.Database.EnsureCreatedAsync();

        var run = await db.PlaybookRuns
            .Include(r => r.StepStates)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run is null)
        {
            throw new InvalidOperationException(
                $"Playbook run with ID {runId} was not found.");
        }

        if (run.CompletedAt is not null)
        {
            return;
        }

        var incompleteSteps = run.StepStates
            .Where(s => !s.IsChecked)
            .OrderBy(s => s.Order)
            .ToList();

        if (incompleteSteps.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot complete this playbook yet. " +
                $"{incompleteSteps.Count} step(s) remain unchecked.");
        }

        run.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private static List<PlaybookStep> CreateOrderedSteps(
        IEnumerable<PlaybookStep>? steps)
    {
        if (steps is null)
        {
            return new List<PlaybookStep>();
        }

        return steps
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select((step, index) => new PlaybookStep
            {
                Order = index,
                Text = step.Text.Trim()
            })
            .ToList();
    }
}