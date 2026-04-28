using System.IO;

namespace WindosRecorder.Services;

public static class DebugLog
{
    private static readonly object SyncRoot = new();

    public static void Write(string message)
    {
        lock (SyncRoot)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "app-debug.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
