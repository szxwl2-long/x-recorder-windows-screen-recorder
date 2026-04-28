using System.Windows;
using System.Windows.Controls;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class RecordingControlWindow : Window
{
    private readonly string _language;
    private bool _allowClose;

    public RecordingControlWindow(string language)
    {
        _language = language;
        InitializeComponent();
        ApplyLanguage(false);
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public event Action? PauseResumeRequested;

    public event Action? StopRequested;

    public event Action<int>? SystemAudioVolumeChanged;

    public event Action<int>? MicrophoneVolumeChanged;

    public event Action? MicrophoneBoostRequested;

    public void AllowClose()
    {
        _allowClose = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 20;
        Top = area.Bottom - Height - 20;
        CaptureProtection.ExcludeFromCapture(this);
    }

    public void SetElapsed(TimeSpan elapsed)
    {
        ElapsedTextBlock.Text = elapsed.ToString(@"hh\:mm\:ss");
    }

    public void SetPaused(bool isPaused)
    {
        ApplyLanguage(isPaused);
    }

    public void SetPauseEnabled(bool enabled)
    {
        PauseResumeButton.IsEnabled = enabled;
    }

    public void SetSystemAudioEnabled(bool enabled)
    {
        SystemAudioVolumePanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetMicrophoneEnabled(bool enabled)
    {
        MicrophoneVolumePanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetSystemAudioVolume(int value)
    {
        SystemAudioSlider.Value = value;
        SystemAudioValueTextBlock.Text = value.ToString();
    }

    public void SetMicrophoneVolume(int value)
    {
        MicrophoneSlider.Value = value;
        MicrophoneValueTextBlock.Text = value.ToString();
    }

    public void SetMicrophoneBoostPercent(int boostPercent)
    {
        MicrophoneBoostButton.Content = $"x{boostPercent / 100}";
    }

    private void ApplyLanguage(bool isPaused)
    {
        Title = LanguageCatalog.Get(_language, "RecordingMiniTitle");
        TitleTextBlock.Text = LanguageCatalog.Get(_language, "RecordingMiniTitle");
        PauseResumeButton.Content = BuildButtonContent(
            isPaused,
            LanguageCatalog.Get(_language, isPaused ? "Resume" : "Pause"));
        StopButtonControl.Content = BuildStopButtonContent(LanguageCatalog.Get(_language, "Stop"));
        SystemAudioVolumeLabelTextBlock.Text = LanguageCatalog.Get(_language, "SystemAudioVolume");
        MicrophoneVolumeLabelTextBlock.Text = LanguageCatalog.Get(_language, "MicrophoneVolume");
        RecordingHintTextBlock.Text = LanguageCatalog.Get(_language, "RecordingHint");
    }

    private static StackPanel BuildButtonContent(bool isPaused, string text)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };

        panel.Children.Add(BuildPauseResumeIcon(isPaused));
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 0, 6, 0),
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        return panel;
    }

    private static StackPanel BuildStopButtonContent(string text)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(BuildStopIcon());
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 0, 6, 0),
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        return panel;
    }

    private static FrameworkElement BuildPauseResumeIcon(bool isPaused)
    {
        var iconBrush = System.Windows.Media.Brushes.Transparent;
        var strokeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(47, 92, 170));

        if (isPaused)
        {
            return new System.Windows.Shapes.Polygon
            {
                Points = new System.Windows.Media.PointCollection([
                    new System.Windows.Point(1, 1),
                    new System.Windows.Point(1, 17),
                    new System.Windows.Point(15, 9)
                ]),
                Fill = iconBrush,
                Stroke = strokeBrush,
                StrokeThickness = 1.8,
                Width = 16,
                Height = 18,
                Stretch = System.Windows.Media.Stretch.Fill,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        var grid = new Grid
        {
            Width = 16,
            Height = 18,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        grid.Children.Add(new Border
        {
            Width = 4,
            Height = 16,
            CornerRadius = new CornerRadius(1),
            BorderThickness = new Thickness(1.8),
            BorderBrush = strokeBrush,
            Background = iconBrush,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        });

        grid.Children.Add(new Border
        {
            Width = 4,
            Height = 16,
            CornerRadius = new CornerRadius(1),
            BorderThickness = new Thickness(1.8),
            BorderBrush = strokeBrush,
            Background = iconBrush,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        });

        return grid;
    }

    private static FrameworkElement BuildStopIcon()
    {
        return new Border
        {
            Width = 15,
            Height = 15,
            CornerRadius = new CornerRadius(2),
            BorderThickness = new Thickness(1.8),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(47, 92, 170)),
            Background = System.Windows.Media.Brushes.Transparent,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        WindowState = WindowState.Minimized;
    }

    private void PauseResumeButton_OnClick(object sender, RoutedEventArgs e)
    {
        PauseResumeRequested?.Invoke();
    }

    private void StopButtonControl_OnClick(object sender, RoutedEventArgs e)
    {
        StopRequested?.Invoke();
    }

    private void SystemAudioSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        var value = (int)Math.Round(SystemAudioSlider.Value);
        SystemAudioValueTextBlock.Text = value.ToString();
        SystemAudioVolumeChanged?.Invoke(value);
    }

    private void MicrophoneSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        var value = (int)Math.Round(MicrophoneSlider.Value);
        MicrophoneValueTextBlock.Text = value.ToString();
        MicrophoneVolumeChanged?.Invoke(value);
    }

    private void SystemAudioMinusButton_OnClick(object sender, RoutedEventArgs e)
    {
        SystemAudioSlider.Value = Math.Max(1, SystemAudioSlider.Value - 1);
    }

    private void SystemAudioPlusButton_OnClick(object sender, RoutedEventArgs e)
    {
        SystemAudioSlider.Value = Math.Min(100, SystemAudioSlider.Value + 1);
    }

    private void MicrophoneMinusButton_OnClick(object sender, RoutedEventArgs e)
    {
        MicrophoneSlider.Value = Math.Max(1, MicrophoneSlider.Value - 1);
    }

    private void MicrophonePlusButton_OnClick(object sender, RoutedEventArgs e)
    {
        MicrophoneSlider.Value = Math.Min(100, MicrophoneSlider.Value + 1);
    }

    private void MicrophoneBoostButton_OnClick(object sender, RoutedEventArgs e)
    {
        MicrophoneBoostRequested?.Invoke();
    }
}
