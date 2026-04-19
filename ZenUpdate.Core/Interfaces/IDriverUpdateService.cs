using ZenUpdate.Core.Models;

namespace ZenUpdate.Core.Interfaces;

/// <summary>
/// Scans for and installs hardware driver updates using the Windows Update API (WUApiLib).
/// Separate from <see cref="IWindowsUpdateService"/> because driver updates have different
/// UI columns and user expectations (manufacturer, device class, etc.).
/// </summary>
public interface IDriverUpdateService
{
    /// <summary>
    /// Searches for available driver updates only (excludes OS software updates).
    /// </summary>
    /// <param name="cancellationToken">Allows the caller to cancel the search.</param>
    /// <returns>A read-only list of available driver updates.</returns>
    Task<IReadOnlyList<DriverUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Downloads and installs a single driver update.
    /// </summary>
    /// <param name="update">The driver update to install.</param>
    /// <param name="progress">Reports installation progress as an integer percentage (0–100).</param>
    /// <param name="cancellationToken">Allows the caller to cancel the installation.</param>
    /// <returns>True if installation succeeded; false otherwise.</returns>
    Task<bool> InstallUpdateAsync(
        DriverUpdateItem update,
        IProgress<int> progress,
        CancellationToken cancellationToken);
}
