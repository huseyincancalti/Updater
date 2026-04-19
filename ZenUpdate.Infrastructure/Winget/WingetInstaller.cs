using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.Infrastructure.Winget;

/// <summary>
/// Installs a single application update using the winget package manager.
/// Implements <see cref="IWingetInstaller"/>.
/// </summary>
public sealed class WingetInstaller : IWingetInstaller
{
    private readonly ProcessRunner _processRunner;
    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new <see cref="WingetInstaller"/> with its required dependencies.
    /// </summary>
    public WingetInstaller(ProcessRunner processRunner, ILoggerService logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> InstallUpdateAsync(
        AppUpdateItem item,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        // TODO: Implement installation.
        // Steps:
        //   1. _logger.Info($"Installing {item.DisplayName} ({item.WingetPackageId})...");
        //   2. Run: winget upgrade --id {item.WingetPackageId} --silent --accept-package-agreements --accept-source-agreements
        //   3. Report progress.Report(100) on completion (or parse output for intermediate progress)
        //   4. Check process exit code — 0 = success
        //   5. Log result and return true/false
        throw new NotImplementedException("WingetInstaller.InstallUpdateAsync is not yet implemented.");
    }
}
