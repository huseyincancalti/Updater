using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using ZenUpdate.App.Services;
using ZenUpdate.App.Startup;
using ZenUpdate.App.ViewModels;
using ZenUpdate.Core.Interfaces;

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

        // Apply the user's saved theme before any window is shown so we do not
        // briefly flash the default palette during startup.
        ApplySavedTheme();

        var mainWindow = new MainWindow();
        var shellVm = Services.GetRequiredService<ShellViewModel>();
        mainWindow.DataContext = shellVm;
        mainWindow.Show();

        // Trigger an auto-scan after the window appears if the user enabled it.
        // We fire-and-forget from the UI thread; ScanAsync is already async-safe.
        _ = TriggerStartupScanAsync(shellVm);
    }

    /// <summary>
    /// Reads the persisted settings once and asks <see cref="IThemeService"/> to apply
    /// the saved <see cref="Core.Enums.AppTheme"/>. Failures fall back to the default
    /// dark theme so a corrupt settings file never blocks startup.
    /// </summary>
    private static void ApplySavedTheme()
    {
        try
        {
            var settings = Services.GetRequiredService<ISettingsRepository>()
                .LoadAsync()
                .GetAwaiter()
                .GetResult();

            Services.GetRequiredService<IThemeService>().ApplyTheme(settings.Theme);
        }
        catch
        {
            // Intentional: theme is cosmetic. Startup must continue even if load fails.
        }
    }

    /// <summary>
    /// Waits for the SettingsViewModel to finish loading from disk,
    /// then triggers a winget scan if <see cref="AppSettings.ScanOnStartup"/> is true.
    /// </summary>
    private static async Task TriggerStartupScanAsync(ShellViewModel shellVm)
    {
        // Give the async InitializeAsync in SettingsViewModel time to complete.
        await Task.Delay(400);

        var settingsVm = Services.GetRequiredService<SettingsViewModel>();
        if (!settingsVm.Settings.ScanOnStartup)
        {
            return;
        }

        var programsVm = Services.GetRequiredService<ProgramsViewModel>();
        if (programsVm.ScanCommand.CanExecute(null))
        {
            // Navigate to Programs so the user sees the scan in progress.
            shellVm.NavigateTo(AppPage.Programs);
            programsVm.ScanCommand.Execute(null);
        }
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
