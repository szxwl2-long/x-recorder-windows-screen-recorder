using ScreenRecorderLib;
using WindosRecorder.Models;

namespace WindosRecorder.Services;

public sealed class ScreenRecordingService
{
    private readonly string _language;
    private Recorder? _recorder;
    private DynamicOptionsBuilder? _dynamicOptionsBuilder;
    private bool _isPaused;

    public ScreenRecordingService(string language = LanguageCatalog.Chinese)
    {
        _language = LanguageCatalog.Normalize(language);
    }

    public bool IsRecording => _recorder is not null;

    public bool IsPaused => _isPaused;

    public event Action<string>? StatusChanged;

    public void Start(RecordingRequest request)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Recorder is already running.");
        }

        var options = BuildOptions(request);
        _recorder = Recorder.CreateRecorder(options);
        _dynamicOptionsBuilder = _recorder.GetDynamicOptionsBuilder();
        _recorder.OnRecordingComplete += (_, e) =>
            StatusChanged?.Invoke(string.Format(LanguageCatalog.Get(_language, "RecordingFinished"), e.FilePath));
        _recorder.OnRecordingFailed += (_, e) =>
            StatusChanged?.Invoke(string.Format(LanguageCatalog.Get(_language, "RecordingFailed"), e.Error));
        _recorder.OnStatusChanged += (_, status) =>
            StatusChanged?.Invoke(string.Format(LanguageCatalog.Get(_language, "RecordingStatus"), status));
        _recorder.Record(request.OutputPath);
        _isPaused = false;
    }

    public Task StopAsync()
    {
        if (_recorder is null)
        {
            return Task.CompletedTask;
        }

        _recorder.Stop();
        _recorder.Dispose();
        _recorder = null;
        _dynamicOptionsBuilder = null;
        _isPaused = false;
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_recorder is null || _isPaused)
        {
            return Task.CompletedTask;
        }

        _recorder.Pause();
        _isPaused = true;
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_recorder is null || !_isPaused)
        {
            return Task.CompletedTask;
        }

        _recorder.Resume();
        _isPaused = false;
        return Task.CompletedTask;
    }

    public void UpdateVolumes(int systemAudioVolume, int microphoneVolume, int microphoneBoostPercent = 100)
    {
        if (_dynamicOptionsBuilder is null)
        {
            return;
        }

        _dynamicOptionsBuilder.SetDynamicAudioOptions(new DynamicAudioOptions
        {
            OutputVolume = systemAudioVolume / 100f,
            InputVolume = (microphoneVolume * microphoneBoostPercent) / 10000f
        });
        _dynamicOptionsBuilder.Apply();
    }

    public static IReadOnlyList<CaptureWindowItem> GetWindows()
    {
        return Recorder.GetWindows()
            .Where(window => !string.IsNullOrWhiteSpace(window.Title))
            .Select(window => new CaptureWindowItem
            {
                Title = $"{window.Title} ({window.Handle})",
                Handle = window.Handle
            })
            .ToList();
    }

    public static IReadOnlyList<MicrophoneItem> GetMicrophones()
    {
        var recorderDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices).ToList();
        using var deviceEnumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
        var captureEndpoints = deviceEnumerator
            .EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active)
            .ToList();
        var microphones = new List<MicrophoneItem>();

        for (var i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
        {
            var capabilities = NAudio.Wave.WaveIn.GetCapabilities(i);
            var match = recorderDevices.FirstOrDefault(device =>
                device.DeviceName.Contains(capabilities.ProductName, StringComparison.OrdinalIgnoreCase) ||
                capabilities.ProductName.Contains(device.DeviceName, StringComparison.OrdinalIgnoreCase));

            var endpoint = captureEndpoints.FirstOrDefault(device =>
                device.FriendlyName.Contains(capabilities.ProductName, StringComparison.OrdinalIgnoreCase) ||
                capabilities.ProductName.Contains(device.FriendlyName, StringComparison.OrdinalIgnoreCase));

            if (match is null && endpoint is not null)
            {
                match = recorderDevices.FirstOrDefault(device =>
                    string.Equals(device.DeviceName, endpoint.ID, StringComparison.OrdinalIgnoreCase));
            }

            if (match is null)
            {
                continue;
            }

            microphones.Add(new MicrophoneItem
            {
                DisplayName = BuildFriendlyMicrophoneName(endpoint?.FriendlyName ?? capabilities.ProductName),
                RecorderDeviceName = match.DeviceName,
                WaveInDeviceNumber = i,
                SourceType = endpoint?.FriendlyName ?? capabilities.ProductName
            });
        }

        if (microphones.Count == 0)
        {
            microphones.AddRange(recorderDevices.Select(device => new MicrophoneItem
            {
                DisplayName = BuildFriendlyMicrophoneName(
                    captureEndpoints.FirstOrDefault(endpoint =>
                        string.Equals(endpoint.ID, device.DeviceName, StringComparison.OrdinalIgnoreCase))?.FriendlyName
                    ?? device.DeviceName),
                RecorderDeviceName = device.DeviceName,
                WaveInDeviceNumber = 0
            }));
        }

        return microphones
            .GroupBy(item => item.RecorderDeviceName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string BuildFriendlyMicrophoneName(string rawName)
    {
        var normalized = rawName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "麦克风";
        }

        normalized = normalized.Replace('（', '(').Replace('）', ')');

        if (normalized.Contains("立体声混音", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("麦克风", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        if (normalized.Contains("Stereo Mix", StringComparison.OrdinalIgnoreCase))
        {
            return ReplaceDeviceType(normalized, "Stereo Mix", "立体声混音");
        }

        if (normalized.Contains("Microphone", StringComparison.OrdinalIgnoreCase))
        {
            return ReplaceDeviceType(normalized, "Microphone", "麦克风");
        }

        if (normalized.Contains("Mic", StringComparison.OrdinalIgnoreCase))
        {
            return ReplaceDeviceType(normalized, "Mic", "麦克风");
        }

        return normalized;
    }

    private static string ReplaceDeviceType(string rawName, string englishType, string chineseType)
    {
        var index = rawName.IndexOf(englishType, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return rawName;
        }

        return string.Concat(
            rawName.AsSpan(0, index),
            chineseType,
            rawName.AsSpan(index + englishType.Length)).Trim();
    }

    private static RecorderOptions BuildOptions(RecordingRequest request)
    {
        var sources = new List<RecordingSourceBase>();

        if (request.Mode == CaptureMode.FullScreen)
        {
            sources.Add(new DisplayRecordingSource(DisplayRecordingSource.MainMonitor)
            {
                IsCursorCaptureEnabled = true
            });
        }
        else if (request.Mode == CaptureMode.Window && request.Window is not null)
        {
            sources.Add(new WindowRecordingSource(request.Window.Handle)
            {
                IsCursorCaptureEnabled = true
            });
        }

        return new RecorderOptions
        {
            SourceOptions = new SourceOptions
            {
                RecordingSources = sources
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = request.IncludeMicrophone || request.IncludeSystemAudio,
                IsInputDeviceEnabled = request.IncludeMicrophone,
                IsOutputDeviceEnabled = request.IncludeSystemAudio,
                AudioInputDevice = request.Microphone?.RecorderDeviceName,
                InputVolume = (request.MicrophoneVolume * request.MicrophoneBoostPercent) / 10000f,
                OutputVolume = request.SystemAudioVolume / 100f
            },
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video,
                OutputFrameSize = new ScreenSize(request.OutputWidth, request.OutputHeight),
                Stretch = StretchMode.Uniform
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                IsHardwareEncodingEnabled = true,
                IsMp4FastStartEnabled = true,
                Bitrate = request.VideoBitrateKbps * 1000,
                Framerate = request.FrameRate,
                IsFixedFramerate = true,
                Encoder = new H264VideoEncoder()
            }
        };
    }
}
