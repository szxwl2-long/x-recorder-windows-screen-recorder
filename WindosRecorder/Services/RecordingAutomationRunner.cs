using System.IO;
using WindosRecorder.Models;

namespace WindosRecorder.Services;

public static class RecordingAutomationRunner
{
    public static async Task RunAsync(AutomationOptions options)
    {
        DebugLog.Write("Automation runner started.");
        Directory.CreateDirectory(options.OutputDirectory);
        var windows = ScreenRecordingService.GetWindows();
        DebugLog.Write($"Automation runner enumerated {windows.Count} window(s).");
        var window = windows.FirstOrDefault(item =>
            item.Title.Contains(options.WindowTitleContains, StringComparison.OrdinalIgnoreCase));

        if (window is null)
        {
            DebugLog.Write($"Automation runner did not find target window: {options.WindowTitleContains}");
            throw new InvalidOperationException(
                string.Format(LanguageCatalog.Get(options.Language, "AutomationWindowNotFound"), options.WindowTitleContains));
        }

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"dodex-record-{DateTime.Now:yyyyMMdd-HHmmss}.mp4");
        DebugLog.Write($"Automation runner output path: {outputPath}");

        var service = new ScreenRecordingService(options.Language);
        service.Start(new RecordingRequest
        {
            Mode = CaptureMode.Window,
            OutputPath = outputPath,
            Window = window,
            IncludeMicrophone = false,
            FrameRate = 30,
            VideoBitrateKbps = 12000,
            OutputWidth = 1920,
            OutputHeight = 1080
        });
        DebugLog.Write("Automation runner recording started.");

        await Task.Delay(TimeSpan.FromSeconds(options.DurationSeconds));
        await service.StopAsync();
        DebugLog.Write("Automation runner requested stop.");
        await Task.Delay(TimeSpan.FromSeconds(2));
        DebugLog.Write("Automation runner completed wait after stop.");
    }
}
