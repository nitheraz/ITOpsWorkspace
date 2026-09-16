using ITOpsWorkspace.App.Services;
using ITOpsWorkspace.App.ViewModels;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Infrastructure.Integrations;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Threading;

namespace ITOpsWorkspace.App;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _idleCheckTimer;
    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private bool _isLocked;

    public MainWindow()
    {
        InitializeComponent();

        PreviewMouseMove += (_, _) => _lastActivityUtc = DateTime.UtcNow;
        PreviewMouseDown += (_, _) => _lastActivityUtc = DateTime.UtcNow;
        PreviewKeyDown += (_, _) => _lastActivityUtc = DateTime.UtcNow;

        _idleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _idleCheckTimer.Tick += IdleCheckTimer_Tick;
        _idleCheckTimer.Start();
    }

    private void IdleCheckTimer_Tick(object? sender, EventArgs e)
    {
        if (_isLocked) return;

        var appSettingsState = App.Services.GetRequiredService<AppSettingsState>();
        var idleThreshold = TimeSpan.FromMinutes(appSettingsState.IdleTimeoutMinutes);

        if (DateTime.UtcNow - _lastActivityUtc >= idleThreshold)
        {
            ShowLockScreen();
        }
    }

    private void ShowLockScreen()
    {
        _isLocked = true;

        var options = App.Services.GetRequiredService<ServiceNowOptions>();
        var userLookupService = App.Services.GetRequiredService<IUserLookupService>();

        var lockViewModel = new LockScreenViewModel(userLookupService, options.InstanceUrl, options.Username);
        var lockScreen = new Views.LockScreen(lockViewModel) { Owner = this };

        var result = lockScreen.ShowDialog();

        if (result == true)
        {
            _lastActivityUtc = DateTime.UtcNow;
            _isLocked = false;
        }
        else
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}