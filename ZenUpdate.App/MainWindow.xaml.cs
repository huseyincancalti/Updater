using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ZenUpdate.App.ViewModels;
using ZenUpdate.Core.Models;

namespace ZenUpdate.App;

/// <summary>
/// Code-behind for the main application window.
/// Kept minimal while hosting small view-only behaviors for the log console.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the MainWindow and its XAML components.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_OnClosing;
    }

    /// <summary>
    /// If <see cref="AppSettings.MinimizeToTray"/> is enabled, hides the window
    /// instead of closing it so the app keeps running in the background.
    /// The user can still exit via Task Manager or a future tray-icon Exit option.
    /// </summary>
    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var settingsVm = App.Services.GetService(typeof(SettingsViewModel)) as SettingsViewModel;
        if (settingsVm?.Settings.MinimizeToTray == true)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void LogConsoleListView_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CopySelectedEntriesToClipboard();
            e.Handled = true;
        }
    }

    private void LogConsoleContextMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        var hasSelection = GetSelectedLogEntries().Count > 0;
        var hasLogs = LogConsoleListView.Items.Count > 0;

        CopySelectedLogMenuItem.IsEnabled = hasSelection;
        CopyAllLogsMenuItem.IsEnabled = hasLogs;
        ClearLogsMenuItem.IsEnabled = hasLogs;
    }

    private void CopySelectedLogMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        CopySelectedEntriesToClipboard();
    }

    private void CopyAllLogsMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        CopyAllEntriesToClipboard();
    }

    private void CopyAllLogsButton_OnClick(object sender, RoutedEventArgs e)
    {
        CopyAllEntriesToClipboard();
    }

    private void ClearLogsMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel shellViewModel)
        {
            shellViewModel.LogConsole.Entries.Clear();
        }
    }

    /// <summary>
    /// Copies every log entry currently in the list view to the clipboard,
    /// using the same text format as the file logger.
    /// </summary>
    private void CopyAllEntriesToClipboard()
    {
        CopyEntriesToClipboard(LogConsoleListView.Items.OfType<LogEntry>());
    }

    private void CopySelectedEntriesToClipboard()
    {
        CopyEntriesToClipboard(GetSelectedLogEntries());
    }

    private void CopyEntriesToClipboard(IEnumerable<LogEntry> entries)
    {
        var lines = entries
            .Select(entry => entry.ToString())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            return;
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    private List<LogEntry> GetSelectedLogEntries()
    {
        return LogConsoleListView.SelectedItems
            .OfType<LogEntry>()
            .OrderBy(entry => LogConsoleListView.Items.IndexOf(entry))
            .ToList();
    }
}
