using Microsoft.EntityFrameworkCore;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Infrastructure.Data;
using ITOpsWorkspace.Infrastructure.Security;

namespace ITOpsWorkspace.Infrastructure.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly string _dbPath;

    public UserSettingsService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public async Task<bool> HasSettingsAsync()
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();
        return await db.Settings.AnyAsync();
    }

    public async Task<UserConnectionSettings?> LoadAsync()
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var entity = await db.Settings.FirstOrDefaultAsync();
        if (entity is null) return null;

        return new UserConnectionSettings
        {
            Username = entity.Username,
            Password = CredentialProtector.Unprotect(entity.EncryptedPassword),
            CurrentUserDisplayName = entity.CurrentUserDisplayName,
            AssignmentGroupName = entity.AssignmentGroupName
        };
    }

    public async Task SaveAsync(UserConnectionSettings settings)
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var entity = await db.Settings.FirstOrDefaultAsync() ?? new SettingsEntity();

        entity.Username = settings.Username;
        entity.EncryptedPassword = CredentialProtector.Protect(settings.Password);
        entity.CurrentUserDisplayName = settings.CurrentUserDisplayName;
        entity.AssignmentGroupName = settings.AssignmentGroupName;

        if (!await db.Settings.AnyAsync(s => s.Id == entity.Id))
            db.Settings.Add(entity);
        else
            db.Settings.Update(entity);

        await db.SaveChangesAsync();
    }

    public async Task ClearAsync()
    {
        using var db = new AppDbContext(_dbPath);
        await db.Database.EnsureCreatedAsync();

        var entity = await db.Settings.FirstOrDefaultAsync();
        if (entity is not null)
        {
            db.Settings.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}