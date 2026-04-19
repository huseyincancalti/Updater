using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using ZenUpdate.App.Startup;
using ZenUpdate.App.ViewModels;

namespace ZenUpdate.App;

/// <summary>
/// Application entry point. Configures the DI container and launches <see cref="MainWindow"/>.
/// This is the only place in the application that knows about concrete service implementations.
/// </summary>
public partial class App : Application
{
    /// <summary>The application-wide DI service provider.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Called by WPF when the application starts.
    /// Builds the service container and opens the main window.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddZenUpdateServices();
        Services = serviceCollection.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.DataContext = Services.GetRequiredService<ShellViewModel>();
        mainWindow.Show();
    }

    /// <summary>
    /// Called when the application is shutting down.
    /// Disposes the service provider to clean up any disposable services.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
