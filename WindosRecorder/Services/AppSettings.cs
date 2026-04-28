namespace WindosRecorder.Services;

public sealed class AppSettings
{
    public string SaveFolderPath { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = "zh";

    public string LastRecordingName { get; set; } = string.Empty;

    public List<string> RecentRecordingNames { get; set; } = [];
}
