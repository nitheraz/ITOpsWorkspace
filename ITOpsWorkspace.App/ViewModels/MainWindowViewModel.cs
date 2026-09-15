using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ITOpsWorkspace.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _currentScreen = "Dashboard";
    [ObservableProperty] private object? _currentViewModel;

    public List<string> NavItems { get; } = new()
    {
        "Dashboard", "My Work", "My Team Work", "Incidents", "Assets",
        "Playbooks", "Knowledge", "Tools", "Reports", "Settings"
    };

    public MainWindowViewModel(DashboardViewModel dashboardViewModel)
    {
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
}