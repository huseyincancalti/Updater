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
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
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
        if (!File.Exists(FilePath))
        {
            _logger.Info("Settings file not found. Using default settings.");
            return new AppSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings is null)
            {
                _logger.Warning("Settings file was empty or invalid. Using default settings.");
                return new AppSettings();
            }

            _logger.Info("Settings loaded successfully.");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Could not load settings. Using defaults. Reason: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var tempFilePath = FilePath + ".tmp";
        var json = JsonSerializer.Serialize(settings, JsonOptions);

        await File.WriteAllTextAsync(tempFilePath, json);
        File.Move(tempFilePath, FilePath, overwrite: true);

        _logger.Info("Settings saved successfully.");
    }
}
