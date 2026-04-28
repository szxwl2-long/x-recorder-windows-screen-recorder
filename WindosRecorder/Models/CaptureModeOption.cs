namespace WindosRecorder.Models;

public sealed class CaptureModeOption
{
    public required string Label { get; init; }

    public required CaptureMode Mode { get; init; }

    public override string ToString()
    {
        return Label;
    }
}
