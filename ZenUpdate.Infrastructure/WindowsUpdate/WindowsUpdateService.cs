using System.Runtime.InteropServices;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.Infrastructure.WindowsUpdate;

/// <summary>
/// Scans for available Windows OS software updates using the Windows Update Agent API (WUApiLib).
/// COM objects are accessed via dynamic late-binding so no COM reference entry is needed in the project file.
/// All COM objects are released in <c>finally</c> blocks to prevent background WUA processes from hanging.
/// </summary>
public sealed class WindowsUpdateService : IWindowsUpdateService
{
    private readonly ILoggerService _logger;

    /// <summary>Initializes a new <see cref="WindowsUpdateService"/>.</summary>
    public WindowsUpdateService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The first call can take 30–120 seconds while Windows Update Agent contacts Microsoft servers.
    /// Subsequent calls in the same session are faster.
    /// </remarks>
    public async Task<IReadOnlyList<WindowsUpdateItem>> GetAvailableUpdatesAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Windows Update scan started.");
        return await Task.Run(() => RunScanOnWorkerThread(cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> InstallUpdateAsync(
        WindowsUpdateItem update,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("WindowsUpdateService.InstallUpdateAsync is not yet implemented.");
    }

    // -------------------------------------------------------------------------
    // Private scan helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Performs the synchronous WUA search on a worker thread.
    /// COM objects are created, used, and released entirely within this method.
    /// </summary>
    private IReadOnlyList<WindowsUpdateItem> RunScanOnWorkerThread(CancellationToken cancellationToken)
    {
        object? session = null;
        object? searcher = null;
        object? searchResult = null;

        try
        {
            var sessionType = Type.GetTypeFromProgID("Microsoft.Update.Session")
                ?? throw new InvalidOperationException(
                    "Windows Update Agent is not available on this machine (ProgID 'Microsoft.Update.Session' not found).");

            session = Activator.CreateInstance(sessionType)!;
            dynamic dynSession = session;

            searcher = dynSession.CreateUpdateSearcher();
            dynamic dynSearcher = searcher;

            TrySetOnline(dynSearcher);

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Info("Querying Windows Update Agent: IsInstalled=0 AND Type='Software' AND IsHidden=0");

            searchResult = dynSearcher.Search("IsInstalled=0 AND Type='Software' AND IsHidden=0");
            dynamic dynResult = searchResult;

            cancellationToken.ThrowIfCancellationRequested();

            var items = MapUpdatesToItems(dynResult.Updates);

            if (items.Count == 0)
                _logger.Info("Windows Update scan complete. No pending updates found — the system is up to date.");
            else
                _logger.Info($"Windows Update scan completed. {items.Count} update(s) found.");

            return items;
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Windows Update scan was cancelled by the user.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error("Windows Update scan encountered an unexpected error.", ex);
            throw;
        }
        finally
        {
            TryReleaseCom(searchResult);
            TryReleaseCom(searcher);
            TryReleaseCom(session);
        }
    }

    /// <summary>
    /// Converts the WUA update collection into a typed list of <see cref="WindowsUpdateItem"/>.
    /// </summary>
    private static List<WindowsUpdateItem> MapUpdatesToItems(dynamic updates)
    {
        var items = new List<WindowsUpdateItem>();
        var count = (int)updates.Count;

        for (var i = 0; i < count; i++)
        {
            dynamic update = updates[i];

            var title    = (string)update.Title;
            var kbId     = GetFirstKbId(update);
            var severity = TryGetSeverity(update);
            var isImportant = severity.Equals("Critical",  StringComparison.OrdinalIgnoreCase)
                           || severity.Equals("Important", StringComparison.OrdinalIgnoreCase);

            items.Add(new WindowsUpdateItem
            {
                Id           = string.IsNullOrEmpty(kbId) ? title : kbId,
                DisplayName  = title,
                KbArticleId  = kbId,
                MsrcSeverity = severity,
                IsImportant  = isImportant,
            });
        }

        return items;
    }

    /// <summary>Returns the first KB article ID prefixed with "KB", or an empty string.</summary>
    private static string GetFirstKbId(dynamic update)
    {
        try
        {
            dynamic kbIds = update.KBArticleIDs;
            if ((int)kbIds.Count == 0) return string.Empty;
            var raw = (string)kbIds[0];
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : "KB" + raw;
        }
        catch
        {
            // KBArticleIDs can throw on some update types — return empty rather than crash.
            return string.Empty;
        }
    }

    /// <summary>Returns the MSRC severity string, or an empty string if unavailable.</summary>
    private static string TryGetSeverity(dynamic update)
    {
        try
        {
            return (string?)update.MsrcSeverity ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Tries to set <c>Online = true</c> on the searcher so it contacts Microsoft servers.
    /// Silently skips if the property is unavailable in the current environment.
    /// </summary>
    private static void TrySetOnline(dynamic searcher)
    {
        try { searcher.Online = true; }
        catch { /* Some environments (e.g. WSUS with online-block policy) restrict this; safe to skip. */ }
    }

    /// <summary>Calls <see cref="Marshal.FinalReleaseComObject"/> on a COM object, ignoring errors.</summary>
    private static void TryReleaseCom(object? obj)
    {
        if (obj is null) return;
        try { Marshal.FinalReleaseComObject(obj); }
        catch { /* Best-effort cleanup; COM objects will still be GC'd if release fails. */ }
    }
}
