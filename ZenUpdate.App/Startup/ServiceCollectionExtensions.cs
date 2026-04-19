using Microsoft.Extensions.DependencyInjection;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Infrastructure.Logging;
using ZenUpdate.Infrastructure.Storage;
using ZenUpdate.Infrastructure.Winget;
using ZenUpdate.Infrastructure.WindowsUpdate;
using ZenUpdate.App.ViewModels;

namespace ZenUpdate.App.Startup;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> that register all
/// ZenUpdate services, repositories, and view models.
/// Called once during application startup in <see cref="App"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static IServiceCollection AddZenUpdateServices(this IServiceCollection services)
    {
        // --- Singletons ---
        // These are created once and shared for the entire lifetime of the application.
        services.AddSingleton<ILoggerService, FileLoggerService>();
        services.AddSingleton<IBlacklistRepository, JsonBlacklistRepository>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();

        // --- Transient (Infrastructure) ---
        // These are stateless helpers — a new instance is fine for each use.
        services.AddTransient<ProcessRunner>();
        services.AddTransient<WingetOutputParser>();
        services.AddTransient<IWingetScanner, WingetScanner>();
        services.AddTransient<IWingetInstaller, WingetInstaller>();
        services.AddTransient<IWindowsUpdateService, WindowsUpdateService>();
        services.AddTransient<IDriverUpdateService, DriverUpdateService>();

        // --- ViewModels (Singleton) ---
        // Singleton preserves state (loaded data, scroll position, etc.)
        // when the user navigates away from a page and comes back.
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<ProgramsViewModel>();
        services.AddSingleton<WindowsUpdatesViewModel>();
        services.AddSingleton<DriversViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<LogConsoleViewModel>();

        return services;
    }
}
