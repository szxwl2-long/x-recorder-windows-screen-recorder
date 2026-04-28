using System.IO;
using System.Text.Json;

namespace WindosRecorder.Services;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindosRecorder");

    private readonly string _settingsPath;

    public AppSettingsService()
    {
        _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return CreateDefaultSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);

            if (settings is null || string.IsNullOrWhiteSpace(settings.SaveFolderPath))
            {
                return CreateDefaultSettings();
            }

            return settings;
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_settingsDirectory);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var defaultFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "WindosRecorder");

        return new AppSettings
        {
            SaveFolderPath = defaultFolder,
            PreferredLanguage = LanguageCatalog.Chinese,
            LastRecordingName = string.Empty,
            RecentRecordingNames = []
        };
    }
}
