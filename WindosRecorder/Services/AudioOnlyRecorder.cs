using NAudio.Wave;
using System.IO;

namespace WindosRecorder.Services;

public sealed class AudioOnlyRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;

    public bool IsRecording => _waveIn is not null;

    public void Start(string outputPath, int deviceNumber)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Audio recorder is already running.");
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(48000, 1),
            BufferMilliseconds = 100
        };

        _writer = new WaveFileWriter(outputPath, _waveIn.WaveFormat);
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;
        _waveIn.StartRecording();
    }

    public Task StopAsync()
    {
        if (!IsRecording)
        {
            return Task.CompletedTask;
        }

        _waveIn!.StopRecording();
        return Task.CompletedTask;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        _writer?.Flush();
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        DisposeInternal();

        if (e.Exception is not null)
        {
            throw e.Exception;
        }
    }

    private void DisposeInternal()
    {
        if (_waveIn is not null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        _writer?.Dispose();
        _writer = null;
    }

    public void Dispose()
    {
        DisposeInternal();
        GC.SuppressFinalize(this);
    }
}
