namespace ZenUpdate.Core.Models;

/// <summary>
/// Stores user-configurable application settings.
/// Serialized to and from <c>%APPDATA%\ZenUpdate\settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// If true, ZenUpdate checks for updates automatically when the app starts.
    /// Default: false (user must trigger scans manually).
    /// </summary>
    public bool ScanOnStartup { get; set; } = false;

    /// <summary>
    /// If true, ZenUpdate minimizes to the system tray instead of closing when the window is closed.
    /// Default: false.
    /// </summary>
    public bool MinimizeToTray { get; set; } = false;

    /// <summary>
    /// The folder where log files are written.
    /// Default: <c>%APPDATA%\ZenUpdate\logs</c>.
    /// </summary>
    public string LogDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZenUpdate", "logs");

    /// <summary>
    /// The maximum number of log entries to display in the UI log console.
    /// Older entries are removed automatically when the limit is exceeded.
    /// Default: 200.
    /// </summary>
    public int MaxLogConsoleEntries { get; set; } = 200;
}
