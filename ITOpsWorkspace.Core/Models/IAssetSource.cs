using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.Core.Interfaces;

public interface IAssetSource
{
    Task<List<Asset>> GetAssetsAsync(AssetQuery? query = null);
    Task<Asset?> GetAssetBySysIdAsync(string sysId);
}