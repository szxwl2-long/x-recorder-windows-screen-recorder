using System.Windows;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class RecordingOverlayWindow : Window
{
    public RecordingOverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    public event Action? PauseResumeRequested;

    public event Action? StopRequested;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        CaptureProtection.ExcludeFromCapture(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        CaptureProtection.ExcludeFromCapture(this);
    }

    public void SetElapsed(TimeSpan elapsed)
    {
        ElapsedTextBlock.Text = elapsed.ToString(@"hh\:mm\:ss");
    }

    public void SetPaused(bool isPaused)
    {
        PauseLeftBar.Visibility = isPaused ? Visibility.Collapsed : Visibility.Visible;
        PauseRightBar.Visibility = isPaused ? Visibility.Collapsed : Visibility.Visible;
        PlayTriangle.Visibility = isPaused ? Visibility.Visible : Visibility.Collapsed;
    }

    public void PositionWindow()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 18;
        Top = area.Bottom - Height - 18;
        CaptureProtection.ExcludeFromCapture(this);
    }

    private void PauseResumeButton_OnClick(object sender, RoutedEventArgs e)
    {
        PauseResumeRequested?.Invoke();
    }

    private void StopButton_OnClick(object sender, RoutedEventArgs e)
    {
        StopRequested?.Invoke();
    }
}
