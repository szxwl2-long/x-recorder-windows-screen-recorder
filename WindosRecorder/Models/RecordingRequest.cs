namespace WindosRecorder.Models;

public sealed class RecordingRequest
{
    public required CaptureMode Mode { get; set; }

    public required string OutputPath { get; set; }

    public CaptureWindowItem? Window { get; set; }

    public MicrophoneItem? Microphone { get; set; }

    public bool IncludeSystemAudio { get; set; }

    public bool IncludeMicrophone { get; set; }

    public int SystemAudioVolume { get; set; } = 100;

    public int MicrophoneVolume { get; set; } = 100;

    public int MicrophoneBoostPercent { get; set; } = 100;

    public int FrameRate { get; set; } = 60;

    public int VideoBitrateKbps { get; set; } = 12000;

    public int OutputWidth { get; set; } = 1920;

    public int OutputHeight { get; set; } = 1080;
}
