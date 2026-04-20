using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the Drivers page.
/// Keeps the future service dependency in place while the UI shows a friendly placeholder.
/// </summary>
public sealed partial class DriversViewModel : ObservableObject
{
    private const string NotImplementedMessage = "This module is not implemented yet.";

    private readonly IDriverUpdateService _service;
    private readonly ILoggerService _logger;

    /// <summary>The list of available driver updates displayed in the DataGrid.</summary>
    public ObservableCollection<DriverUpdateItem> Updates { get; } = new();

    /// <summary>True while a scan or install is running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Short status message shown below the page content.</summary>
    [ObservableProperty]
    private string _statusMessage = NotImplementedMessage;

    /// <summary>
    /// Initializes the DriversViewModel with its required services.
    /// </summary>
    public DriversViewModel(IDriverUpdateService service, ILoggerService logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Handles the current placeholder action for the Drivers page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    private void Scan()
    {
        _ = _service;
        Updates.Clear();
        StatusMessage = NotImplementedMessage;
        _logger.Info("Drivers module is not implemented yet.");
    }

    private bool CanScan() => !IsBusy;
}
