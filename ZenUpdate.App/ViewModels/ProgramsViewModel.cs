using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Programs page.
/// Drives the winget scan flow: calls <see cref="IWingetScanner"/>,
/// populates the <see cref="Updates"/> collection, and manages busy state.
/// </summary>
public sealed partial class ProgramsViewModel : ObservableObject
{
    private readonly IWingetScanner _scanner;
    private readonly IWingetInstaller _installer;
    private readonly ILoggerService _logger;

    // Allows an in-progress scan to be cancelled.
    private CancellationTokenSource? _scanCts;

    // -------------------------------------------------------------------------
    // Observable properties (CommunityToolkit source generator picks these up)
    // -------------------------------------------------------------------------

    /// <summary>True while a scan operation is running. Disables the Scan button.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallSelectedCommand))]
    private bool _isBusy;

    /// <summary>Short status line shown below the DataGrid.</summary>
    [ObservableProperty]
    private string _statusMessage = "Press 'Scan for Updates' to start.";

    // -------------------------------------------------------------------------
    // Collections
    // -------------------------------------------------------------------------

    /// <summary>
    /// The list of available application updates displayed in the DataGrid.
    /// Always populated on the UI thread.
    /// </summary>
    public ObservableCollection<AppUpdateItem> Updates { get; } = new();

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes the ViewModel with its required services.
    /// All services are injected by the DI container.
    /// </summary>
    public ProgramsViewModel(
        IWingetScanner scanner,
        IWingetInstaller installer,
        ILoggerService logger)
    {
        _scanner   = scanner;
        _installer = installer;
        _logger    = logger;
    }

    // -------------------------------------------------------------------------
    // Commands
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scans for available winget updates in the background.
    /// The UI thread is never blocked — all heavy work runs off-thread.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        // Create a fresh CancellationTokenSource for this scan run.
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();

        IsBusy = true;
        StatusMessage = "Scanning for updates…";

        // Clear the previous results immediately so the DataGrid shows empty.
        Updates.Clear();

        try
        {
            // GetAvailableUpdatesAsync is internally async (process I/O),
            // so we do not need Task.Run here.
            var results = await _scanner.GetAvailableUpdatesAsync(_scanCts.Token);

            // Always marshal ObservableCollection updates back to the UI thread.
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in results)
                    Updates.Add(item);
            });

            StatusMessage = Updates.Count == 0
                ? "All applications are up to date."
                : $"{Updates.Count} update(s) available.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
            _logger.Info("Programs scan was cancelled by the user.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed — see the log for details.";
            _logger.Error("Programs scan encountered an unexpected error.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The scan command is only enabled when no scan is already running.</summary>
    private bool CanScan() => !IsBusy;

    /// <summary>Cancels an in-progress scan.</summary>
    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
        StatusMessage = "Cancelling scan…";
    }

    /// <summary>
    /// Installs all updates where <see cref="AppUpdateItem.IsSelected"/> is true.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallSelectedAsync()
    {
        var selected = Updates.Where(u => u.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsBusy = true;
        StatusMessage = $"Installing {selected.Count} update(s)…";

        try
        {
            foreach (var item in selected)
            {
                item.Status = Core.Enums.UpdateStatus.Installing;
                var progress = new Progress<int>(); // wire up per-row progress bar later
                var success = await _installer.InstallUpdateAsync(item, progress, CancellationToken.None);
                item.Status = success
                    ? Core.Enums.UpdateStatus.Succeeded
                    : Core.Enums.UpdateStatus.Failed;
            }
            StatusMessage = "Installation complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Installation failed — see the log for details.";
            _logger.Error("Install encountered an unexpected error.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanInstall() => !IsBusy && Updates.Any(u => u.IsSelected);
}
