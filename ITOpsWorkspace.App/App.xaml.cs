using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.App.ViewModels;
using ITOpsWorkspace.App.Views;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Infrastructure.Integrations;
using ITOpsWorkspace.Infrastructure.Services;

namespace ITOpsWorkspace.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevent WPF from shutting the app down between dialog windows
        // (Org Setup -> Login -> MainWindow) when no window is briefly open.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var orgSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ITOpsWorkspace", "org-settings.json");
        var orgSettingsService = new OrgSettingsService(orgSettingsPath);

        var hasOrgSettings = await orgSettingsService.HasSettingsAsync();
        if (!hasOrgSettings)
        {
            var orgSetupViewModel = new OrgSetupViewModel(orgSettingsService);
            var orgSetupWindow = new OrgSetupWindow(orgSetupViewModel);
            var orgResult = orgSetupWindow.ShowDialog();

            if (orgResult != true)
            {
                Shutdown();
                return;
            }
        }

        var orgSettings = await orgSettingsService.LoadAsync();
        if (orgSettings is null)
        {
            MessageBox.Show("Organization setup did not complete correctly. Please restart the application.");
            Shutdown();
            return;
        }

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ITOpsWorkspace");
        Directory.CreateDirectory(userDataFolder);
        var userDbPath = Path.Combine(userDataFolder, "itopsworkspace.db");

        var userSettingsService = new UserSettingsService(userDbPath);
        var userLookupService = new ServiceNowUserLookupService();

        var hasUserSettings = await userSettingsService.HasSettingsAsync();
        if (!hasUserSettings)
        {
            var loginViewModel = new LoginViewModel(userSettingsService, userLookupService, orgSettings.InstanceUrl);
            var loginWindow = new LoginWindow(loginViewModel);
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Shutdown();
                return;
            }
        }

        var userSettings = await userSettingsService.LoadAsync();
        if (userSettings is null)
        {
            MessageBox.Show("Login did not complete correctly. Please restart the application.");
            Shutdown();
            return;
        }

        var services = new ServiceCollection();

        services.AddSingleton<IOrgSettingsService>(orgSettingsService);
        services.AddSingleton<IUserSettingsService>(userSettingsService);
        services.AddSingleton<IUserLookupService>(userLookupService);
        services.AddSingleton(orgSettings);
        services.AddSingleton(userSettings);

        var options = new ServiceNowOptions
        {
            InstanceUrl = orgSettings.InstanceUrl,
            Username = userSettings.Username,
            Password = userSettings.Password
        };
        services.AddSingleton(options);

        services.AddSingleton(new CurrentUserContext(userSettings.CurrentUserDisplayName));
        services.AddSingleton(new TeamContext(userSettings.AssignmentGroupName));

        services.AddHttpClient<IIncidentSource, ServiceNowIncidentSource>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MyWorkViewModel>();
        services.AddTransient<MyTeamWorkViewModel>();
        services.AddTransient<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainWindowViewModel>()
        };

        MainWindow = mainWindow; // register as the app's real main window
        ShutdownMode = ShutdownMode.OnMainWindowClose; // now safe to shut down when this closes
        mainWindow.Show();
    }
}