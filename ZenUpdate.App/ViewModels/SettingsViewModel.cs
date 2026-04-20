using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>The package ID entered for a new blacklist entry.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddBlacklistEntryCommand))]
    private string _newBlacklistPackageId = string.Empty;

    /// <summary>The optional reason entered for a new blacklist entry.</summary>
    [ObservableProperty]
    private string _newBlacklistReason = string.Empty;

    /// <summary>The currently selected blacklist entry in the UI.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedBlacklistEntryCommand))]
    private BlacklistEntry? _selectedBlacklistEntry;

    /// <summary>Short feedback text shown on the Settings page.</summary>
    [ObservableProperty]
    private string _statusMessage = "Settings loaded.";

    /// <summary>The list of blacklist entries shown in the Settings UI.</summary>
    public ObservableCollection<BlacklistEntry> BlacklistEntries { get; } = new();

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

        _ = InitializeAsync();
    }

    /// <summary>
    /// Reloads settings and blacklist entries from disk.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            // Unsubscribe first so we don't auto-save while loading.
            if (Settings is not null)
            {
                Settings.PropertyChanged -= OnSettingsPropertyChanged;
            }

            Settings = await _settingsRepo.LoadAsync();
            Settings.PropertyChanged += OnSettingsPropertyChanged;

            await ReloadBlacklistEntriesAsync();
            StatusMessage = "Settings loaded.";
            _logger.Info("Settings loaded.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not load settings. See log for details.";
            _logger.Error("Settings page failed to load data.", ex);
        }
    }

    /// <summary>
    /// Saves the current application settings to disk.
    /// </summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        try
        {
            await _settingsRepo.SaveAsync(Settings);
            StatusMessage = "Settings saved.";
            _logger.Info("Settings saved by user.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not save settings. See log for details.";
            _logger.Error("Settings save failed.", ex);
        }
    }

    /// <summary>
    /// Adds a new blacklist entry using the package ID and optional reason entered by the user.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddBlacklistEntry))]
    public async Task AddBlacklistEntryAsync()
    {
        var packageId = NewBlacklistPackageId.Trim();
        if (string.IsNullOrWhiteSpace(packageId))
        {
            StatusMessage = "Package ID is required.";
            return;
        }

        if (BlacklistEntries.Any(entry => string.Equals(entry.PackageId, packageId, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "That package ID is already blacklisted.";
            return;
        }

        try
        {
            await _blacklistRepo.AddAsync(packageId, NewBlacklistReason);
            await ReloadBlacklistEntriesAsync();

            NewBlacklistPackageId = string.Empty;
            NewBlacklistReason = string.Empty;
            StatusMessage = $"Added '{packageId}' to blacklist.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not add blacklist entry. See log for details.";
            _logger.Error("Blacklist add failed.", ex);
        }
    }

    /// <summary>
    /// Removes the currently selected blacklist entry.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRemoveSelectedBlacklistEntry))]
    public async Task RemoveSelectedBlacklistEntryAsync()
    {
        if (SelectedBlacklistEntry is null)
        {
            return;
        }

        var packageId = SelectedBlacklistEntry.PackageId;

        try
        {
            await _blacklistRepo.RemoveAsync(packageId);
            await ReloadBlacklistEntriesAsync();
            SelectedBlacklistEntry = null;
            StatusMessage = $"Removed '{packageId}' from blacklist.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Could not remove blacklist entry. See log for details.";
            _logger.Error("Blacklist remove failed.", ex);
        }
    }

    private bool CanAddBlacklistEntry()
    {
        return !string.IsNullOrWhiteSpace(NewBlacklistPackageId);
    }

    private bool CanRemoveSelectedBlacklistEntry()
    {
        return SelectedBlacklistEntry is not null;
    }

    /// <summary>
    /// Automatically saves settings when a checkbox value changes,
    /// so the user does not have to click "Save Settings" for boolean toggles.
    /// </summary>
    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppSettings.ScanOnStartup) or nameof(AppSettings.MinimizeToTray))
        {
            _ = SaveAsync();
        }
    }

    private async Task InitializeAsync()
    {
        await LoadAsync();
    }

    private async Task ReloadBlacklistEntriesAsync()
    {
        var entries = await _blacklistRepo.GetEntriesAsync();

        BlacklistEntries.Clear();
        foreach (var entry in entries)
        {
            BlacklistEntries.Add(entry);
        }

        _logger.Info($"Blacklist loaded: {BlacklistEntries.Count} entry(ies).");
    }
}
