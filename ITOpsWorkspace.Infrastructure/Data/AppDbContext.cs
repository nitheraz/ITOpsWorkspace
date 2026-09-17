using Microsoft.EntityFrameworkCore;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    public DbSet<Playbook> Playbooks => Set<Playbook>();
    public DbSet<PlaybookStep> PlaybookSteps => Set<PlaybookStep>();
    public DbSet<PlaybookRun> PlaybookRuns => Set<PlaybookRun>();
    public DbSet<PlaybookRunStepState> PlaybookRunStepStates => Set<PlaybookRunStepState>();

    private readonly string _dbPath;

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Playbook>()
            .HasMany(p => p.Steps)
            .WithOne()
            .HasForeignKey(s => s.PlaybookId);

        modelBuilder.Entity<PlaybookRun>()
            .HasMany(r => r.StepStates)
            .WithOne()
            .HasForeignKey(s => s.PlaybookRunId);
    }
}