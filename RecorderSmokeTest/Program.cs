using NAudio.Wave;
using ScreenRecorderLib;

var outputDir = Path.Combine(Environment.CurrentDirectory, "smoke-output");
Directory.CreateDirectory(outputDir);
var outputPath = Path.Combine(outputDir, $"smoke-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");
var audioPath = Path.Combine(outputDir, $"smoke-audio-{DateTime.Now:yyyyMMdd-HHmmss}.wav");

var recordingCompleted = new TaskCompletionSource<string>();
var recordingFailed = new TaskCompletionSource<string>();

var recorder = Recorder.CreateRecorder(new RecorderOptions
{
    SourceOptions = new SourceOptions
    {
        RecordingSources =
        [
            new DisplayRecordingSource(DisplayRecordingSource.MainMonitor)
            {
                IsCursorCaptureEnabled = true
            }
        ]
    },
    AudioOptions = new AudioOptions
    {
        IsAudioEnabled = false,
        IsInputDeviceEnabled = false,
        IsOutputDeviceEnabled = false
    },
    OutputOptions = new OutputOptions
    {
        RecorderMode = RecorderMode.Video,
        OutputFrameSize = new ScreenSize(1920, 1080),
        Stretch = StretchMode.Uniform
    },
    VideoEncoderOptions = new VideoEncoderOptions
    {
        IsHardwareEncodingEnabled = true,
        IsMp4FastStartEnabled = true,
        Bitrate = 6000 * 1000,
        Framerate = 30,
        IsFixedFramerate = true,
        Encoder = new H264VideoEncoder()
    }
});

recorder.OnRecordingComplete += (_, e) => recordingCompleted.TrySetResult(e.FilePath);
recorder.OnRecordingFailed += (_, e) => recordingFailed.TrySetResult(e.Error);

recorder.Record(outputPath);
await Task.Delay(TimeSpan.FromSeconds(3));
recorder.Stop();

var completedTask = await Task.WhenAny(
    recordingCompleted.Task,
    recordingFailed.Task,
    Task.Delay(TimeSpan.FromSeconds(15)));

if (completedTask == recordingCompleted.Task)
{
    var completedPath = await recordingCompleted.Task;
    var info = new FileInfo(completedPath);
    Console.WriteLine($"VIDEO_OK|{completedPath}|{info.Length}");
}
else if (completedTask == recordingFailed.Task)
{
    Console.WriteLine($"VIDEO_FAIL|{await recordingFailed.Task}");
}
else
{
    Console.WriteLine("VIDEO_TIMEOUT");
}

Console.WriteLine($"MIC_COUNT|{WaveIn.DeviceCount}");
for (var i = 0; i < WaveIn.DeviceCount; i++)
{
    var capabilities = WaveIn.GetCapabilities(i);
    Console.WriteLine($"MIC|{i}|{capabilities.ProductName}");
}

if (WaveIn.DeviceCount > 0)
{
    using var waveIn = new WaveInEvent
    {
        DeviceNumber = 0,
        WaveFormat = new WaveFormat(48000, 1)
    };
    using var writer = new WaveFileWriter(audioPath, waveIn.WaveFormat);

    waveIn.DataAvailable += (_, e) =>
    {
        writer.Write(e.Buffer, 0, e.BytesRecorded);
        writer.Flush();
    };

    waveIn.StartRecording();
    await Task.Delay(TimeSpan.FromSeconds(2));
    waveIn.StopRecording();
    await Task.Delay(500);

    var audioInfo = new FileInfo(audioPath);
    Console.WriteLine($"AUDIO_OK|{audioPath}|{audioInfo.Length}");
}
else
{
    Console.WriteLine("AUDIO_SKIPPED|NO_MIC");
}
