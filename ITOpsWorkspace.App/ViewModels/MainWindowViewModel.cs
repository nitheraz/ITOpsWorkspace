using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.Core.Interfaces;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IUserSettingsService _userSettingsService;

    [ObservableProperty] private string _currentScreen = "Dashboard";
    [ObservableProperty] private object? _currentViewModel;

    public List<string> NavItems { get; } = new()
    {
        "Dashboard", "My Work", "My Team Work", "Incidents", "Assets",
        "Playbooks", "Knowledge", "Tools", "Reports", "Settings"
    };

    public MainWindowViewModel(DashboardViewModel dashboardViewModel, IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
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
            _ => null
        };
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _userSettingsService.ClearAsync();

        // Relaunch as a fresh process so the app restarts cleanly at Login,
        // rather than trying to tear down and rebuild the DI container in place.
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(exePath))
            Process.Start(exePath);

        System.Windows.Application.Current.Shutdown();
    }
}