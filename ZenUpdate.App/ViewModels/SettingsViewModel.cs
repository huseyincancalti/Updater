using CommunityToolkit.Mvvm.ComponentModel;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Settings page.
/// Manages user preferences and the blacklist.
/// Binds to <c>SettingsView.xaml</c>.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepo;
    private readonly IBlacklistRepository _blacklistRepo;
    private readonly ILoggerService _logger;

    /// <summary>The currently loaded application settings, bound to the Settings form.</summary>
    [ObservableProperty]
    private AppSettings _settings = new();

    /// <summary>The list of currently blacklisted package IDs, shown in the blacklist table.</summary>
    public System.Collections.ObjectModel.ObservableCollection<string> BlacklistedIds { get; } = new();

    /// <summary>
    /// Initializes the SettingsViewModel with its required repositories.
    /// </summary>
    public SettingsViewModel(
        ISettingsRepository settingsRepo,
        IBlacklistRepository blacklistRepo,
        ILoggerService logger)
    {
        _settingsRepo = settingsRepo;
        _blacklistRepo = blacklistRepo;
        _logger = logger;
    }

    // TODO: Add LoadAsync(), SaveAsync(), AddToBlacklist(), RemoveFromBlacklist() commands.
}
