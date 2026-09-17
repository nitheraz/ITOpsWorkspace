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
        _dbPath = dbPath;
    }

    public async Task<List<Playbook>> GetAllPlaybooksAsync()
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();
        return await db.Playbooks.Include(p => p.Steps).ToListAsync();
    }

    public async Task<Playbook?> GetPlaybookAsync(int id)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();
        return await db.Playbooks.Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SavePlaybookAsync(Playbook playbook)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        if (playbook.Id == 0)
        {
            db.Playbooks.Add(playbook);
        }
        else
        {
            var existing = await db.Playbooks.Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == playbook.Id);
            if (existing is null)
            {
                db.Playbooks.Add(playbook);
            }
            else
            {
                existing.Title = playbook.Title;
                existing.Description = playbook.Description;
                db.PlaybookSteps.RemoveRange(existing.Steps);
                existing.Steps = playbook.Steps;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task DeletePlaybookAsync(int id)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var playbook = await db.Playbooks.Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == id);
        if (playbook is not null)
        {
            db.Playbooks.Remove(playbook);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Playbook>> SearchPlaybooksAsync(string keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords)) return new List<Playbook>();

        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var words = keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 3)
            .ToList();

        if (words.Count == 0) return new List<Playbook>();

        var all = await db.Playbooks.Include(p => p.Steps).ToListAsync();

        return all.Where(p => words.Any(w =>
                p.Title.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<PlaybookRun> StartRunAsync(int playbookId, string incidentSysId, string incidentNumber)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var playbook = await db.Playbooks.Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == playbookId)
            ?? throw new InvalidOperationException("Playbook not found.");

        var run = new PlaybookRun
        {
            PlaybookId = playbook.Id,
            PlaybookTitle = playbook.Title,
            IncidentSysId = incidentSysId,
            IncidentNumber = incidentNumber,
            StartedAt = DateTime.UtcNow,
            StepStates = playbook.Steps
                .OrderBy(s => s.Order)
                .Select(s => new PlaybookRunStepState
                {
                    PlaybookStepId = s.Id,
                    StepText = s.Text,
                    Order = s.Order,
                    IsChecked = false
                })
                .ToList()
        };

        db.PlaybookRuns.Add(run);
        await db.SaveChangesAsync();

        return run;
    }

    public async Task<PlaybookRun?> GetActiveRunAsync(string incidentSysId)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        return await db.PlaybookRuns
            .Include(r => r.StepStates)
            .Where(r => r.IncidentSysId == incidentSysId && r.CompletedAt == null)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SetStepCheckedAsync(int runStepStateId, bool isChecked)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var state = await db.PlaybookRunStepStates.FirstOrDefaultAsync(s => s.Id == runStepStateId);
        if (state is null) return;

        state.IsChecked = isChecked;
        state.CheckedAt = isChecked ? DateTime.UtcNow : null;

        await db.SaveChangesAsync();
    }

    public async Task CompleteRunAsync(int runId)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var run = await db.PlaybookRuns.FirstOrDefaultAsync(r => r.Id == runId);
        if (run is null) return;

        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}