using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using ZenUpdate.App.Services;
using ZenUpdate.App.Startup;
using ZenUpdate.App.ViewModels;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

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
    /// Every step is individually guarded so a bad settings file or a broken
    /// theme resource never prevents <see cref="MainWindow"/> from showing.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddZenUpdateServices();
            Services = serviceCollection.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] DI container build failed: {ex}");
            ShowFallbackWindow("ZenUpdate could not initialize its services.\n\n" + ex.Message);
            return;
        }

        // Cosmetic: pre-apply the saved theme before any window is shown.
        // Failures here are swallowed inside ApplySavedTheme so they cannot
        // block the MainWindow.Show() call below.
        ApplySavedTheme();

        MainWindow? mainWindow = null;
        try
        {
            mainWindow = new MainWindow();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] MainWindow construction failed: {ex}");
            ShowFallbackWindow("ZenUpdate could not load its main window.\n\n" + ex.Message);
            return;
        }

        ShellViewModel? shellVm = null;
        try
        {
            shellVm = Services.GetRequiredService<ShellViewModel>();
            mainWindow.DataContext = shellVm;
        }
        catch (Exception ex)
        {
            // The window will still appear (empty); the user can at least close it.
            Debug.WriteLine($"[ZenUpdate] ShellViewModel resolution failed: {ex}");
        }

        try
        {
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] MainWindow.Show failed: {ex}");
            ShowFallbackWindow("ZenUpdate could not display its main window.\n\n" + ex.Message);
            return;
        }

        if (shellVm is not null)
        {
            // Trigger an auto-scan after the window appears if the user enabled it.
            // We fire-and-forget from the UI thread; ScanAsync is already async-safe.
            _ = TriggerStartupScanAsync(shellVm);
        }
    }

    /// <summary>
    /// Reads the persisted settings once and asks <see cref="IThemeService"/> to apply
    /// the saved <see cref="Core.Enums.AppTheme"/>. Failures fall back to the default
    /// dark theme so a corrupt settings file never blocks startup.
    /// </summary>
    private static void ApplySavedTheme()
    {
        AppSettings settings;
        try
        {
            // CRITICAL: We are on the WPF dispatcher thread. Calling
            // `.GetAwaiter().GetResult()` directly on an async file read
            // here deadlocks: the awaited continuation tries to resume on
            // the dispatcher SynchronizationContext that we are blocking.
            // Wrapping the call in Task.Run escapes the dispatcher context
            // so the I/O continuation runs on a thread-pool thread instead.
            // This is the real reason the previous startup hung whenever
            // %APPDATA%\ZenUpdate\settings.json existed.
            settings = Task.Run(static () =>
                Services.GetRequiredService<ISettingsRepository>().LoadAsync()
            ).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] Settings load failed at startup, using defaults: {ex}");
            settings = new AppSettings();
        }

        try
        {
            Services.GetRequiredService<IThemeService>().ApplyTheme(settings.Theme);
        }
        catch (Exception ex)
        {
            // Theme is purely cosmetic. Startup must continue even if the
            // theme dictionaries cannot be merged for any reason.
            Debug.WriteLine($"[ZenUpdate] Theme apply failed at startup: {ex}");
        }
    }

    /// <summary>
    /// Last-ditch recovery window so the app never silently exits when something
    /// catastrophic happens during <see cref="OnStartup"/>.
    /// </summary>
    private static void ShowFallbackWindow(string message)
    {
        try
        {
            new Window
            {
                Title = "ZenUpdate (Recovery)",
                Width = 480,
                Height = 220,
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    Margin = new Thickness(16),
                    TextWrapping = TextWrapping.Wrap
                }
            }.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] Recovery window failed to show: {ex}");
        }
    }

    /// <summary>
    /// Waits for the SettingsViewModel to finish loading from disk,
    /// then triggers a winget scan if <see cref="AppSettings.ScanOnStartup"/> is true.
    /// </summary>
    private static async Task TriggerStartupScanAsync(ShellViewModel shellVm)
    {
        try
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[ZenUpdate] Startup scan trigger failed: {ex}");
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
