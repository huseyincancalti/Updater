using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.Infrastructure.WindowsUpdate;

/// <summary>
/// Scans for and installs hardware driver updates using the Windows Update Agent API (WUApiLib).
/// Implements <see cref="IDriverUpdateService"/>.
/// Differs from <see cref="WindowsUpdateService"/> only in the WUApiLib search criteria
/// (Type='Driver' instead of Type='Software').
/// </summary>
public sealed class DriverUpdateService : IDriverUpdateService
{
    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new <see cref="DriverUpdateService"/>.
    /// </summary>
    public DriverUpdateService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DriverUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement WUApiLib scan for driver updates.
        // Use the same pattern as WindowsUpdateService, but with criteria:
        // "IsInstalled=0 AND Type='Driver'"
        throw new NotImplementedException("DriverUpdateService.GetAvailableUpdatesAsync is not yet implemented.");
    }

    /// <inheritdoc />
    public async Task<bool> InstallUpdateAsync(
        DriverUpdateItem update,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        // TODO: Implement WUApiLib download + install for drivers.
        throw new NotImplementedException("DriverUpdateService.InstallUpdateAsync is not yet implemented.");
    }
}
