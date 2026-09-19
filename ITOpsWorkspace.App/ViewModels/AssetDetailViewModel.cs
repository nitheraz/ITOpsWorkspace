using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class AssetDetailViewModel : ObservableObject
{
    private readonly object _originViewModel;
    private readonly INavigationService _navigationService;

    public Asset Asset { get; }

    public AssetDetailViewModel(Asset asset, object originViewModel, INavigationService navigationService)
    {
        Asset = asset;
        _originViewModel = originViewModel;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateTo(_originViewModel);
    }
}