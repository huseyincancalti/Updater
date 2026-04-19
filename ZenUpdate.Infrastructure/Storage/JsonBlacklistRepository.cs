using System.Text.Json;
using ZenUpdate.Core.Interfaces;

namespace ZenUpdate.Infrastructure.Storage;

/// <summary>
/// Reads and writes the blacklist JSON file stored at
/// <c>%APPDATA%\ZenUpdate\blacklist.json</c>.
///
/// The file contains a simple JSON array of package ID strings:
/// <code>["Microsoft.Teams", "Zoom.Zoom"]</code>
///
/// A <see cref="SemaphoreSlim"/> prevents concurrent file writes.
/// </summary>
public sealed class JsonBlacklistRepository : IBlacklistRepository
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZenUpdate", "blacklist.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Limits concurrent file access to one operation at a time.
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly ILoggerService _logger;

    /// <summary>Initializes the repository with the logger service.</summary>
    public JsonBlacklistRepository(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetBlacklistedIdsAsync()
    {
        if (!File.Exists(FilePath))
            return Array.Empty<string>();

        await _lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                   ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.Warning($"Could not read blacklist file. Returning empty list. Reason: {ex.Message}");
            return Array.Empty<string>();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(string packageId)
    {
        await _lock.WaitAsync();
        try
        {
            var current = await ReadListUnsafeAsync();
            if (!current.Any(id => string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase)))
            {
                current.Add(packageId);
                await WriteListUnsafeAsync(current);
                _logger.Info($"Added '{packageId}' to blacklist.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string packageId)
    {
        await _lock.WaitAsync();
        try
        {
            var current = await ReadListUnsafeAsync();
            int removed = current.RemoveAll(id =>
                string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                await WriteListUnsafeAsync(current);
                _logger.Info($"Removed '{packageId}' from blacklist.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // Must be called while _lock is already held.
    private async Task<List<string>> ReadListUnsafeAsync()
    {
        if (!File.Exists(FilePath))
            return new List<string>();

        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    // Must be called while _lock is already held.
    private async Task WriteListUnsafeAsync(List<string> list)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(list, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json);
    }
}
