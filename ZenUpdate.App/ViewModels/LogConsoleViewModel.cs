using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZenUpdate.Core.Enums;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App.ViewModels;

/// <summary>
/// ViewModel for the log console drawer displayed at the bottom of the main window.
/// Subscribes to <see cref="ILoggerService.LogEntryAdded"/> and appends entries
/// to a collection bound to the UI.
/// </summary>
public sealed partial class LogConsoleViewModel : ObservableObject
{
    private const int MaxEntries = 200;

    private readonly ILoggerService _logger;

    /// <summary>
    /// The collection of recent log entries displayed in the UI log panel.
    /// Bound to the ListView in the log drawer.
    /// </summary>
    public ObservableCollection<LogEntry> Entries { get; } = new();

    /// <summary>
    /// Whether the log drawer is currently expanded. Remembered for the current app session.
    /// Default is collapsed so the log panel doesn't take permanent screen space.
    /// </summary>
    [ObservableProperty]
    private bool _isOpen;

    /// <summary>
    /// How many error-level entries are currently in the list.
    /// Drives the small error badge shown on the drawer header when the drawer is collapsed.
    /// </summary>
    [ObservableProperty]
    private int _errorCount;

    /// <summary>True when at least one error-level entry is present.</summary>
    public bool HasErrors => ErrorCount > 0;

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

        Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    /// <summary>Toggles the drawer open/closed state.</summary>
    [RelayCommand]
    private void ToggleOpen()
    {
        IsOpen = !IsOpen;
    }

    /// <summary>Removes every entry and resets the error indicator.</summary>
    [RelayCommand]
    private void ClearEntries()
    {
        Entries.Clear();
    }

    partial void OnErrorCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasErrors));
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

            // Auto-open the drawer when an error appears, so the user sees it immediately.
            if (entry.Severity == LogSeverity.Error)
            {
                IsOpen = true;
            }
        });
    }

    /// <summary>
    /// Keeps <see cref="ErrorCount"/> in sync with the current entries list.
    /// </summary>
    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var errors = 0;
        foreach (var entry in Entries)
        {
            if (entry.Severity == LogSeverity.Error)
            {
                errors++;
            }
        }

        ErrorCount = errors;
    }
}
