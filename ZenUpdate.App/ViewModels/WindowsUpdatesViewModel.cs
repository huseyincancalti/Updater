using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Windows Updates page.
/// Drives the WUA scan flow, displays available updates, and manages busy state.
/// Install functionality is not implemented in this phase.
/// </summary>
public sealed partial class WindowsUpdatesViewModel : ObservableObject
{
    private readonly IWindowsUpdateService _service;
    private readonly ILoggerService _logger;

    private CancellationTokenSource? _scanCts;

    /// <summary>The list of available OS updates shown in the DataGrid.</summary>
    public ObservableCollection<WindowsUpdateItem> Updates { get; } = new();

    /// <summary>True while a scan is running.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isBusy;

    /// <summary>Short status line shown below the DataGrid.</summary>
    [ObservableProperty]
    private string _statusMessage = "Press 'Check for Updates' to scan.";

    /// <summary>Initializes the ViewModel with its required services.</summary>
    public WindowsUpdatesViewModel(IWindowsUpdateService service, ILoggerService logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Searches for available Windows Updates in the background.
    /// The first run can take up to two minutes while WUA contacts Microsoft servers.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        if (IsBusy) return;

        ResetCancellation();

        IsBusy = true;
        Updates.Clear();
        StatusMessage = "Scanning for Windows Updates\u2026 This may take up to two minutes.";

        try
        {
            var results = await _service.GetAvailableUpdatesAsync(_scanCts!.Token);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in results)
                    Updates.Add(item);
            });

            StatusMessage = Updates.Count == 0
                ? "Windows is up to date."
                : $"{Updates.Count} update(s) available.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed. See the log for details.";
            _logger.Error("Windows Update scan failed.", ex);
        }
        finally
        {
            IsBusy = false;
            ScanCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanScan() => !IsBusy;

    /// <summary>Cancels the currently running scan.</summary>
    [RelayCommand]
    private void CancelScan()
    {
        if (!IsBusy || _scanCts is null) return;
        StatusMessage = "Cancelling\u2026";
        _scanCts.Cancel();
    }

    private void ResetCancellation()
    {
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
    }
}
