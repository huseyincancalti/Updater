using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Drivers page.
/// Scans for and installs hardware driver updates via <see cref="IDriverUpdateService"/>.
/// Binds to <c>DriversView.xaml</c>.
/// </summary>
public sealed partial class DriversViewModel : ObservableObject
{
    private readonly IDriverUpdateService _service;
    private readonly ILoggerService _logger;

    /// <summary>The list of available driver updates displayed in the DataGrid.</summary>
    public ObservableCollection<DriverUpdateItem> Updates { get; } = new();

    /// <summary>True while a scan or install is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Short status message shown below the DataGrid.</summary>
    [ObservableProperty]
    private string _statusMessage = "Press 'Scan' to check for driver updates.";

    /// <summary>
    /// Initializes the DriversViewModel with its required services.
    /// </summary>
    public DriversViewModel(IDriverUpdateService service, ILoggerService logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Scans for available driver updates in the background.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusMessage = "Searching for driver updates...";
        Updates.Clear();

        try
        {
            _logger.Info("Driver scan started.");
            var results = await Task.Run(() => _service.GetAvailableUpdatesAsync(CancellationToken.None));

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var item in results)
                    Updates.Add(item);
            });

            StatusMessage = Updates.Count == 0
                ? "All drivers are up to date."
                : $"{Updates.Count} driver update(s) available.";

            _logger.Info($"Driver scan complete. {Updates.Count} update(s) found.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Scan failed. See the log for details.";
            _logger.Error("Driver scan failed.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanScan() => !IsBusy;
}
