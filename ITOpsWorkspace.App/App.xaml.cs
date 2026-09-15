using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ITOpsWorkspace.App.ViewModels;
using ITOpsWorkspace.Core.Interfaces;
using ITOpsWorkspace.Core.Models;
using ITOpsWorkspace.Infrastructure.Integrations;

namespace ITOpsWorkspace.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .AddUserSecrets<App>()
            .Build();

        var services = new ServiceCollection();

        var options = new ServiceNowOptions
        {
            InstanceUrl = config["ServiceNow:InstanceUrl"] ?? "",
            Username = config["ServiceNow:Username"] ?? "",
            Password = config["ServiceNow:Password"] ?? ""
        };
        services.AddSingleton(options);

        var currentUserName = config["CurrentUser:DisplayName"] ?? "";
        services.AddSingleton(new CurrentUserContext(currentUserName));

        var teamMembers = config.GetSection("Team:Members").Get<List<string>>() ?? new List<string>();
        services.AddSingleton(new TeamContext(teamMembers));

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
        mainWindow.Show();
    }
}