using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Windows Updates page.
/// Scans for and installs OS updates via <see cref="IWindowsUpdateService"/>.
/// Binds to <c>WindowsUpdatesView.xaml</c>.
/// </summary>
public sealed partial class WindowsUpdatesViewModel : ObservableObject
{
    private readonly IWindowsUpdateService _service;
    private readonly ILoggerService _logger;

    /// <summary>The list of available OS updates displayed in the DataGrid.</summary>
    public ObservableCollection<WindowsUpdateItem> Updates { get; } = new();

    /// <summary>True while a scan or install is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Short status message shown below the DataGrid.</summary>
    [ObservableProperty]
    private string _statusMessage = "Press 'Scan' to check for Windows Updates.";

    /// <summary>
    /// Initializes the WindowsUpdatesViewModel with its required services.
    /// </summary>
    public WindowsUpdatesViewModel(IWindowsUpdateService service, ILoggerService logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Scans for available Windows OS updates in the background.
    /// Note: This can take 30–120 seconds on first run.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Searching for Windows Updates (this may take a minute)...";
        Updates.Clear();

        try
        {
            _logger.Info("Windows Update scan started.");
            var results = await Task.Run(() => _service.GetAvailableUpdatesAsync(CancellationToken.None));

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in results)
                    Updates.Add(item);
            });

            StatusMessage = Updates.Count == 0
                ? "Windows is up to date."
                : $"{Updates.Count} update(s) available.";

            _logger.Info($"Windows Update scan complete. {Updates.Count} update(s) found.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed. See the log for details.";
            _logger.Error("Windows Update scan failed.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanScan() => !IsBusy;
}
