using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the log console panel displayed at the bottom of the main window.
/// Subscribes to <see cref="ILoggerService.LogEntryAdded"/> and appends entries
/// to a collection bound to the UI.
/// </summary>
public sealed partial class LogConsoleViewModel : ObservableObject
{
    private const int MaxEntries = 200;

    private readonly ILoggerService _logger;

    /// <summary>
    /// The collection of recent log entries displayed in the UI log panel.
    /// Bound to the <c>LogConsoleControl</c> ListView.
    /// </summary>
    public ObservableCollection<LogEntry> Entries { get; } = new();

    /// <summary>
    /// Initializes the LogConsoleViewModel and subscribes to logger events.
    /// </summary>
    public LogConsoleViewModel(ILoggerService logger)
    {
        _logger = logger;

        // Subscribe to the logger's event.
        // IMPORTANT: This callback may be raised from a background thread.
        // We must dispatch to the UI thread before touching the ObservableCollection.
        _logger.LogEntryAdded += OnLogEntryAdded;
    }

    /// <summary>
    /// Handles a new log entry from the logger service.
    /// Dispatches the UI update to the main thread.
    /// </summary>
    private void OnLogEntryAdded(LogEntry entry)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Entries.Insert(0, entry); // Newest entries appear at the top.

            // Keep the collection from growing indefinitely.
            while (Entries.Count > MaxEntries)
                Entries.RemoveAt(Entries.Count - 1);
        });
    }
}
