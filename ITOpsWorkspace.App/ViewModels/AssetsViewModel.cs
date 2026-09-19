using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class AssetsViewModel : ObservableObject
{
    private readonly IAssetSource _assetSource;
    private readonly INavigationService _navigationService;

    private List<Asset> _allAssets = new();
    private const int PageSize = 10;

    public ObservableCollection<Asset> Assets { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private int _totalPages = 1;

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public AssetsViewModel(IAssetSource assetSource, INavigationService navigationService)
    {
        _assetSource = assetSource;
        _navigationService = navigationService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        _allAssets = await _assetSource.GetAssetsAsync(new AssetQuery { Keywords = SearchText });
        CurrentPage = 1;
        UpdateTotalPages();
        UpdatePageItems();
        IsLoading = false;
    }

    [RelayCommand]
    private async Task Search()
    {
        await LoadAsync();
    }

    private void UpdateTotalPages()
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling(_allAssets.Count / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
    }

    private void UpdatePageItems()
    {
        Assets.Clear();
        foreach (var asset in _allAssets.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Assets.Add(asset);
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages) { CurrentPage++; UpdatePageItems(); }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1) { CurrentPage--; UpdatePageItems(); }
    }

    [RelayCommand]
    private void OpenAsset(Asset asset)
    {
        _navigationService.NavigateTo(new AssetDetailViewModel(asset, this, _navigationService));
    }
}