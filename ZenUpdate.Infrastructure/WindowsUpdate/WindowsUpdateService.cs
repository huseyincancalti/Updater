using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.Infrastructure.WindowsUpdate;

/// <summary>
/// Scans for and installs Windows OS software updates using the Windows Update Agent API (WUApiLib).
/// Implements <see cref="IWindowsUpdateService"/>.
/// </summary>
/// <remarks>
/// IMPORTANT: All WUApiLib COM objects MUST be released in finally blocks using
/// <c>System.Runtime.InteropServices.Marshal.FinalReleaseComObject()</c>
/// to prevent background Windows Update processes from hanging after the app closes.
/// </remarks>
public sealed class WindowsUpdateService : IWindowsUpdateService
{
    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new <see cref="WindowsUpdateService"/>.
    /// </summary>
    public WindowsUpdateService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WindowsUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement WUApiLib scan for OS software updates.
        // Steps:
        //   1. Create IUpdateSession (WUApiLib.UpdateSession)
        //   2. Create IUpdateSearcher from session
        //   3. Set search criteria: "IsInstalled=0 AND Type='Software'"
        //   4. Run search (can take 30-120 seconds — must be in Task.Run())
        //   5. Map IUpdate objects to WindowsUpdateItem records
        //   6. Release all COM objects in finally{}
        throw new NotImplementedException("WindowsUpdateService.GetAvailableUpdatesAsync is not yet implemented.");
    }

    /// <inheritdoc />
    public async Task<bool> InstallUpdateAsync(
        WindowsUpdateItem update,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        // TODO: Implement WUApiLib download + install.
        throw new NotImplementedException("WindowsUpdateService.InstallUpdateAsync is not yet implemented.");
    }
}
