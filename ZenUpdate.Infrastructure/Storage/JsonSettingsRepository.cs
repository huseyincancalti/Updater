using System.Text.Json;
using ZenUpdate.Core.Interfaces;
using ZenUpdate.Core.Models;

namespace ZenUpdate.Infrastructure.Storage;

/// <summary>
/// Reads and writes the application settings JSON file at <c>%APPDATA%\ZenUpdate\settings.json</c>.
/// Implements <see cref="ISettingsRepository"/>.
/// </summary>
public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZenUpdate", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true // Makes the file human-readable for manual editing
    };

    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new <see cref="JsonSettingsRepository"/>.
    /// </summary>
    public JsonSettingsRepository(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        // TODO: Implement settings loading.
        // Steps:
        //   1. If file doesn't exist, return new AppSettings() (defaults)
        //   2. Deserialize JSON — wrap in try-catch
        //   3. On deserialization failure, log error and return defaults (never crash on startup)
        throw new NotImplementedException("JsonSettingsRepository.LoadAsync is not yet implemented.");
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        // TODO: Implement settings saving.
        // Steps:
        //   1. Ensure directory exists
        //   2. Serialize with WriteIndented = true
        //   3. Write to file atomically (write to temp file, then rename)
        throw new NotImplementedException("JsonSettingsRepository.SaveAsync is not yet implemented.");
    }
}
