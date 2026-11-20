using System.Text.Json;
using System.Text.Json.Serialization;
using NyxMonitor.Core.Abstractions;
using NyxMonitor.Core.Configuration;

namespace NyxMonitor.Infrastructure.Services;

/// <summary>Persists <see cref="AppSettings"/> as JSON under %AppData%\BuiltByNyx\NyxMonitor.</summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IAppLogger _log;
    private readonly string _path;

    public JsonSettingsService(IAppLogger log)
    {
        _log = log;
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BuiltByNyx", "NyxMonitor", "settings.json");
        Current = Load();
    }

    public AppSettings Current { get; }

    public string SettingsPath => _path;

    public void Save()
    {
        try
        {
            Current.Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Current, Options));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex)
        {
            _log.Warning("Could not save settings.", ex);
        }
    }

    private AppSettings Load()
    {
        AppSettings settings;
        bool exists = File.Exists(_path);
        try
        {
            settings = exists
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _log.Warning("Settings file unreadable; using defaults.", ex);
            settings = new AppSettings();
            exists = true; // do not overwrite a file the user may want to repair by hand
        }

        settings.Normalize();

        if (!exists)
        {
            // Write defaults once so the file is easy to find and edit.
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
            }
            catch (Exception ex)
            {
                _log.Warning("Could not write default settings.", ex);
            }
        }

        return settings;
    }
}
