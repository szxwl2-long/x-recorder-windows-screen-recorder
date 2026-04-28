namespace WindosRecorder.Models;

public sealed class AutomationOptions
{
    public string Language { get; init; } = "zh";

    public required string WindowTitleContains { get; init; }

    public required string OutputDirectory { get; init; }

    public int DurationSeconds { get; init; } = 10;
}
