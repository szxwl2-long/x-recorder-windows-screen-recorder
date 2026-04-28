namespace WindosRecorder.Models;

public sealed class ResolutionOption
{
    public required string Label { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public override string ToString()
    {
        return Label;
    }
}
