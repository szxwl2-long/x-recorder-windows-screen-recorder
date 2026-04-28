namespace WindosRecorder.Models;

public sealed class CaptureWindowItem
{
    public required string Title { get; init; }

    public nint Handle { get; init; }

    public override string ToString()
    {
        return Title;
    }
}
