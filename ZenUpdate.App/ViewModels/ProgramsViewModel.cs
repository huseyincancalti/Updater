using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Enums;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Programs page.
/// Drives the winget scan flow, selected update flow, and page busy state.
/// </summary>
public sealed partial class ProgramsViewModel : ObservableObject
{
    private readonly IWingetScanner _scanner;
    private readonly IWingetInstaller _installer;
    private readonly IBlacklistRepository _blacklistRepository;
    private readonly ILoggerService _logger;

    private CancellationTokenSource? _operationCts;

    /// <summary>True while a scan or update batch is running.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallSelectedCommand))]
    private bool _isBusy;

    /// <summary>Short status line shown below the DataGrid.</summary>
    [ObservableProperty]
    private string _statusMessage = "Press 'Scan for Updates' to start.";

    /// <summary>True while the selected-app Winget update batch is running.</summary>
    [ObservableProperty]
    private bool _isUpdateBatchRunning;

    /// <summary>Shows the current item position within the selected update batch.</summary>
    [ObservableProperty]
    private string _currentBatchProgressText = string.Empty;

    /// <summary>Shows the display name of the application currently being updated.</summary>
    [ObservableProperty]
    private string _currentAppName = string.Empty;

    /// <summary>Shows extra progress detail for the current application update.</summary>
    [ObservableProperty]
    private string _currentInstallDetailText = string.Empty;

    /// <summary>
    /// The list of available application updates displayed in the DataGrid.
    /// Always populated on the UI thread.
    /// </summary>
    public ObservableCollection<AppUpdateItem> Updates { get; } = new();

    /// <summary>
    /// Initializes the ViewModel with its required services.
    /// All services are injected by the DI container.
    /// </summary>
    public ProgramsViewModel(
        IWingetScanner scanner,
        IWingetInstaller installer,
        IBlacklistRepository blacklistRepository,
        ILoggerService logger)
    {
        _scanner = scanner;
        _installer = installer;
        _blacklistRepository = blacklistRepository;
        _logger = logger;

        Updates.CollectionChanged += OnUpdatesCollectionChanged;
    }

    /// <summary>
    /// Scans for available winget updates in the background.
    /// The UI thread is never blocked because winget process I/O is awaited asynchronously.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (IsBusy)
        {
            _logger.Info("Scan request ignored because another operation is already running.");
            return;
        }

        ResetOperationCancellation();
        ClearUpdateFeedback();

        IsBusy = true;
        StatusMessage = "Scanning for updates...";
        Updates.Clear();

        try
        {
            var results = await _scanner.GetAvailableUpdatesAsync(_operationCts!.Token);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in results)
                {
                    item.Status = UpdateStatus.Pending;
                    Updates.Add(item);
                }
            });

            StatusMessage = Updates.Count == 0
                ? "All applications are up to date."
                : $"{Updates.Count} update(s) available.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
            _logger.Info("Programs scan was cancelled by the user.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed. See the log for details.";
            _logger.Error("Programs scan encountered an unexpected error.", ex);
        }
        finally
        {
            CompleteOperation();
        }
    }

    private bool CanScan() => !IsBusy;

    /// <summary>Cancels the currently running scan or update batch.</summary>
    [RelayCommand]
    private void CancelScan()
    {
        if (!IsBusy || _operationCts is null)
        {
            return;
        }

        StatusMessage = "Cancelling operation...";

        if (IsUpdateBatchRunning)
        {
            CurrentInstallDetailText = "Cancelling current Winget operation...";
        }

        _operationCts.Cancel();
    }

    /// <summary>
    /// Updates all applications where <see cref="AppUpdateItem.IsSelected"/> is true.
    /// Selected items are processed one by one.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallSelectedAsync()
    {
        if (IsBusy)
        {
            _logger.Warning("Update request ignored because another operation is already running.");
            return;
        }

        var selectedItems = Updates.Where(item => item.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            StatusMessage = "No applications selected.";
            _logger.Info("Update Selected clicked with no applications selected.");
            return;
        }

        ResetOperationCancellation();

        IsBusy = true;
        IsUpdateBatchRunning = true;
        _logger.Info("Winget update batch started.");
        _logger.Info($"{selectedItems.Count} application(s) selected for update.");

        var succeededCount = 0;
        var failedCount = 0;

        try
        {
            for (var index = 0; index < selectedItems.Count; index++)
            {
                _operationCts!.Token.ThrowIfCancellationRequested();

                var item = selectedItems[index];
                await ShowInstallingStateAsync(item, index, selectedItems.Count);

                try
                {
                    var progress = new Progress<int>(OnInstallProgressReported);
                    var success = await _installer.InstallUpdateAsync(item, progress, _operationCts.Token);

                    item.Status = success ? UpdateStatus.Succeeded : UpdateStatus.Failed;

                    if (success)
                    {
                        succeededCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    item.Status = UpdateStatus.Pending;
                    throw;
                }
            }

            StatusMessage = $"Update complete. {succeededCount} succeeded, {failedCount} failed.";
            _logger.Info($"Winget update batch completed. Total: {selectedItems.Count}, Succeeded: {succeededCount}, Failed: {failedCount}.");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Update cancelled. {succeededCount} succeeded, {failedCount} failed.";
            _logger.Info($"Winget update batch was cancelled. Completed before cancel: {succeededCount + failedCount} of {selectedItems.Count}.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Update batch failed. See the log for details.";
            _logger.Error("Winget update batch encountered an unexpected error.", ex);
        }
        finally
        {
            CompleteOperation();
        }
    }

    private bool CanInstall() => !IsBusy && Updates.Any(item => item.IsSelected);

    /// <summary>
    /// Adds the given program rows to the blacklist and removes newly blacklisted items from the visible list.
    /// </summary>
    /// <param name="items">The program rows to blacklist.</param>
    /// <returns>The number of package IDs newly added to the blacklist.</returns>
    public async Task<int> AddItemsToBlacklistAsync(IEnumerable<AppUpdateItem> items)
    {
        var candidates = items
            .Where(item => item is not null && !string.IsNullOrWhiteSpace(item.WingetPackageId))
            .GroupBy(item => item.WingetPackageId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (candidates.Count == 0)
        {
            StatusMessage = "No package IDs available to blacklist.";
            return 0;
        }

        var existingIds = await _blacklistRepository.GetBlacklistedIdsAsync();
        var knownIds = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedIds = new List<string>();

        foreach (var item in candidates)
        {
            if (!knownIds.Add(item.WingetPackageId))
            {
                continue;
            }

            await _blacklistRepository.AddAsync(item.WingetPackageId);
            addedIds.Add(item.WingetPackageId);
        }

        if (addedIds.Count == 0)
        {
            StatusMessage = "Selected package IDs are already blacklisted.";
            return 0;
        }

        RemoveVisibleItemsByPackageId(addedIds);

        StatusMessage = addedIds.Count == 1
            ? $"Added '{addedIds[0]}' to blacklist."
            : $"Added {addedIds.Count} package ID(s) to blacklist.";

        _logger.Info($"Programs page added {addedIds.Count} package ID(s) to blacklist.");
        return addedIds.Count;
    }

    private async Task ShowInstallingStateAsync(AppUpdateItem item, int currentIndex, int totalCount)
    {
        item.Status = UpdateStatus.Installing;
        CurrentBatchProgressText = $"Updating {currentIndex + 1} of {totalCount}...";
        CurrentAppName = item.DisplayName;
        CurrentInstallDetailText = "Waiting for winget to finish...";
        StatusMessage = $"{CurrentBatchProgressText} Current app: {item.DisplayName}";

        await Task.Yield();
    }

    private void OnInstallProgressReported(int percent)
    {
        if (percent is > 0 and < 100)
        {
            CurrentInstallDetailText = $"Winget reported {percent}%...";
        }
        else if (percent >= 100)
        {
            CurrentInstallDetailText = "Finalizing update...";
        }
    }

    private void OnUpdatesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<AppUpdateItem>())
            {
                item.PropertyChanged -= OnUpdateItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<AppUpdateItem>())
            {
                item.PropertyChanged += OnUpdateItemPropertyChanged;
            }
        }

        InstallSelectedCommand.NotifyCanExecuteChanged();
    }

    private void OnUpdateItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppUpdateItem.IsSelected))
        {
            InstallSelectedCommand.NotifyCanExecuteChanged();
        }
    }

    private void RemoveVisibleItemsByPackageId(IEnumerable<string> packageIds)
    {
        var idSet = packageIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var itemsToRemove = Updates
            .Where(item => idSet.Contains(item.WingetPackageId))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            Updates.Remove(item);
        }
    }

    private void ResetOperationCancellation()
    {
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
    }

    private void CompleteOperation()
    {
        IsBusy = false;
        InstallSelectedCommand.NotifyCanExecuteChanged();
        ScanCommand.NotifyCanExecuteChanged();
        ClearUpdateFeedback();
    }

    private void ClearUpdateFeedback()
    {
        IsUpdateBatchRunning = false;
        CurrentBatchProgressText = string.Empty;
        CurrentAppName = string.Empty;
        CurrentInstallDetailText = string.Empty;
    }
}
