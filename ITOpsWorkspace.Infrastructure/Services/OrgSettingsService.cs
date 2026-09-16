using System.Text.Json;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Infrastructure.Services;

public class OrgSettingsService : IOrgSettingsService
{
    private readonly string _filePath;

    public OrgSettingsService(string filePath)
    {
        _filePath = filePath;
    }

    public Task<bool> HasSettingsAsync() => Task.FromResult(File.Exists(_filePath));

    public async Task<OrgConnectionSettings?> LoadAsync()
    {
        if (!File.Exists(_filePath)) return null;

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<OrgConnectionSettings>(json);
    }

    public async Task SaveAsync(OrgConnectionSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_filePath, json);
    }
}