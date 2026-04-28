using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WindosRecorder.Services;

public static class CaptureProtection
{
    private const uint WdaNone = 0;
    private const uint WdaExcludeFromCapture = 0x00000011;

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    public static void ExcludeFromCapture(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);
        }
    }

    public static void Clear(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            SetWindowDisplayAffinity(handle, WdaNone);
        }
    }
}
