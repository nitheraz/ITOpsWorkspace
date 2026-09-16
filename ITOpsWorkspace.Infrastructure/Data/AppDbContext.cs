using Microsoft.EntityFrameworkCore;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<SettingsEntity> Settings => Set<SettingsEntity>();
    //public DbSet<PriorityFlag> PriorityFlags => Set<PriorityFlag>();

    private readonly string _dbPath;

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }
}