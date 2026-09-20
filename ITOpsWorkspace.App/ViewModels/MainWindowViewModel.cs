using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.Core.Enums;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string _currentScreen = "Dashboard";
    [ObservableProperty] private object? _currentViewModel;

    public List<string> NavItems { get; }

    public MainWindowViewModel(DashboardViewModel dashboardViewModel, IUserSettingsService userSettingsService,
        INavigationService navigationService, UserConnectionSettings userSettings)
    {
        _userSettingsService = userSettingsService;
        _navigationService = navigationService;

        _navigationService.CurrentViewModelChanged += vm => CurrentViewModel = vm;

        var items = new List<string> { "Dashboard", "My Work" };

        if (userSettings.Role is UserRole.ITManager or UserRole.Administrator)
            items.Add("My Team Work");

        items.AddRange(new[] { "Incidents", "Assets", "Playbooks", "Knowledge", "Tools", "Reports", "Settings" });
        NavItems = items;

        CurrentViewModel = dashboardViewModel;
    }

    [RelayCommand]
    private void NavigateTo(string screen)
    {
        CurrentScreen = screen;
        CurrentViewModel = screen switch
        {
            "Dashboard" => App.Services.GetRequiredService<DashboardViewModel>(),
            "My Work" => App.Services.GetRequiredService<MyWorkViewModel>(),
            "My Team Work" => App.Services.GetRequiredService<MyTeamWorkViewModel>(),
            "Assets" => App.Services.GetRequiredService<AssetsViewModel>(),
            "Playbooks" => App.Services.GetRequiredService<PlaybooksViewModel>(),
            "Settings" => App.Services.GetRequiredService<SettingsViewModel>(),
            _ => null
        };
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _userSettingsService.ClearAsync();

        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(exePath))
            System.Diagnostics.Process.Start(exePath);

        System.Windows.Application.Current.Shutdown();
    }
}