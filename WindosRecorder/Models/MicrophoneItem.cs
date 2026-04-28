namespace WindosRecorder.Models;

public sealed class MicrophoneItem
{
    public required string DisplayName { get; init; }

    public required string RecorderDeviceName { get; init; }

    public int WaveInDeviceNumber { get; init; } = -1;

    public string? SourceType { get; init; }

    public override string ToString()
    {
        return DisplayName;
    }
}
