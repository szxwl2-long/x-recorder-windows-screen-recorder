using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Threading;
using WindosRecorder.Models;
using WindosRecorder.Services;
using MessageBox = System.Windows.MessageBox;

namespace WindosRecorder;

public partial class MainWindow : Window
{
    private readonly string _language;
    private readonly ScreenRecordingService _screenRecordingService;
    private readonly AudioOnlyRecorder _audioOnlyRecorder = new();
    private readonly AppSettingsService _settingsService = new();
    private readonly ObservableCollection<CaptureModeOption> _modes = [];
    private readonly ObservableCollection<CaptureWindowItem> _windows = [];
    private readonly ObservableCollection<MicrophoneItem> _microphones = [];
    private readonly ObservableCollection<ResolutionOption> _resolutions = [];
    private readonly ObservableCollection<string> _recentRecordingNames = [];
    private readonly Stopwatch _recordingStopwatch = new();
    private readonly DispatcherTimer _elapsedTimer;

    private AppSettings _settings = new();
    private RecordingControlWindow? _recordingControlWindow;
    private TimeSpan _pausedElapsed = TimeSpan.Zero;
    private bool _isClosing;
    private bool _isSyncingControlWindow;
    private bool _isUpdatingBitrateRecommendation;
    private bool _hasAppliedInitialBitrateRecommendation;
    private bool _isInitializingUi;
    private int _lastRecommendedBitrate;
    private int _microphoneBoostPercent = 100;

    public MainWindow(string language)
    {
        _language = LanguageCatalog.Normalize(language);
        _screenRecordingService = new ScreenRecordingService(_language);
        InitializeComponent();
        Loaded += OnLoadedAsync;
        Closing += OnClosing;
        Closed += OnClosed;
        _screenRecordingService.StatusChanged += message => Dispatcher.Invoke(() => StatusTextBlock.Text = message);
        _elapsedTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _elapsedTimer.Tick += (_, _) => UpdateElapsedUi();
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        DebugLog.Write("MainWindow loaded event started.");
        _isInitializingUi = true;
        _settings = _settingsService.Load();
        _settings.PreferredLanguage = _language;
        _settingsService.Save(_settings);

        Directory.CreateDirectory(_settings.SaveFolderPath);

        _modes.Add(new CaptureModeOption { Label = T("ModeFullScreen"), Mode = CaptureMode.FullScreen });
        _modes.Add(new CaptureModeOption { Label = T("ModeWindow"), Mode = CaptureMode.Window });
        _modes.Add(new CaptureModeOption { Label = T("ModeAudioOnly"), Mode = CaptureMode.AudioOnly });
        ModeComboBox.ItemsSource = _modes;
        ModeComboBox.SelectedIndex = 0;

        FrameRateComboBox.ItemsSource = new[] { 30, 60 };
        FrameRateComboBox.SelectedIndex = 1;

        _resolutions.Add(new ResolutionOption { Label = "2K (2560 x 1440)", Width = 2560, Height = 1440 });
        _resolutions.Add(new ResolutionOption { Label = "1080P (1920 x 1080)", Width = 1920, Height = 1080 });
        _resolutions.Add(new ResolutionOption { Label = "720P (1280 x 720)", Width = 1280, Height = 720 });
        _resolutions.Add(new ResolutionOption { Label = "480P (854 x 480)", Width = 854, Height = 480 });
        ResolutionComboBox.ItemsSource = _resolutions;
        ResolutionComboBox.SelectedIndex = 1;

        WindowComboBox.ItemsSource = _windows;
        MicrophoneComboBox.ItemsSource = _microphones;
        RecordingNameComboBox.ItemsSource = _recentRecordingNames;
        HookRecordingNameEditor();

        SaveFolderTextBox.Text = _settings.SaveFolderPath;
        ReplaceRecordingNameHistory(_settings.RecentRecordingNames);
        RecordingNameComboBox.Text = _settings.LastRecordingName ?? string.Empty;
        ApplyLanguage();
        UpdateModeUi();
        ApplyRecommendedBitrateForResolution();
        StatusTextBlock.Text = T("LoadingDevices");

        try
        {
            var windowsTask = Task.Run(ScreenRecordingService.GetWindows);
            var microphonesTask = Task.Run(ScreenRecordingService.GetMicrophones);
            var windows = await windowsTask;
            var microphones = await microphonesTask;

            _windows.Clear();
            foreach (var window in windows)
            {
                _windows.Add(window);
            }

            _microphones.Clear();
            foreach (var microphone in microphones)
            {
                _microphones.Add(microphone);
            }

            if (_windows.Count > 0)
            {
                WindowComboBox.SelectedIndex = 0;
            }

            if (_microphones.Count > 0)
            {
                MicrophoneComboBox.SelectedIndex = 0;
            }

            StatusTextBlock.Text = T("Ready");
            DebugLog.Write($"MainWindow initialization completed. Windows={_windows.Count}, Microphones={_microphones.Count}");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"MainWindow initialization failed: {ex}");
            MessageBox.Show(this, ex.Message, T("StartupFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown(-1);
        }
        finally
        {
            _isInitializingUi = false;
        }
    }

    private string T(string key)
    {
        return LanguageCatalog.Get(_language, key);
    }

    private void ApplyLanguage()
    {
        Title = T("AppTitle").Replace("WINDOS", "X");
        HeroTitleTextBlock.Text = T("HeroTitle").Replace("WINDOS", "X");
        HeroSubtitleTextBlock.Text = T("HeroSubtitle");
        SettingsTitleTextBlock.Text = T("SettingsTitle");
        FullScreenModeTitleTextBlock.Text = T("ModeFullScreen");
        WindowModeTitleTextBlock.Text = T("ModeWindow");
        AudioOnlyModeTitleTextBlock.Text = T("ModeAudioOnly");
        ModeLabelTextBlock.Text = T("ModeLabel");
        MicrophoneLabelTextBlock.Text = T("MicrophoneLabel");
        WindowLabelTextBlock.Text = T("WindowLabel");
        RefreshWindowsButton.Content = T("RefreshWindows");
        ChooseWindowButton.Content = T("ChooseWindowButton");
        IncludeSystemAudioCheckBox.Content = T("IncludeSystemAudio");
        IncludeMicrophoneCheckBox.Content = T("IncludeMic");
        SystemAudioCardTitleTextBlock.Text = T("SystemAudioCardTitle");
        SystemAudioDescriptionTextBlock.Text = T("SystemAudioDescription");
        SystemAudioVolumeLabelTextBlock.Text = T("SystemAudioVolume");
        MicrophoneVolumeLabelTextBlock.Text = T("MicrophoneVolume");
        FrameRateLabelTextBlock.Text = T("FrameRate");
        ResolutionLabelTextBlock.Text = T("Resolution");
        BitrateLabelTextBlock.Text = T("Bitrate");
        SaveTitleTextBlock.Text = T("SaveTitle");
        SaveDescriptionTextBlock.Text = T("SaveDescription");
        ChooseFolderButton.Content = T("ChooseFolder");
        RecordingNameLabelTextBlock.Text = T("RecordingName");
        RecordingNameHintTextBlock.Text = T("RecordingNameHint");
        BitrateHintTextBlock.Text = T("BitrateHint");
        OpenSaveFolderButton.Content = T("OpenSaveFolder");
        AuthorTextBlock.Text = T("AuthorLabel");
        SupportAuthorTitleTextBlock.Text = T("SupportAuthorTitle");
        SupportAuthorSubtitleTextBlock.Text = T("SupportAuthorSubtitle");
        TipsTitleTextBlock.Text = T("TipsTitle");
        TipsLineOneTextBlock.Text = T("TipsLineOne");
        TipsLineTwoTextBlock.Text = T("TipsLineTwo");
        StatusLabelTextBlock.Text = T("Status");
        StatusTextBlock.Text = T("Ready");
        StartButton.Content = T("Start");
        UpdateSelectedWindowText();
        UpdateStorageEstimate();
    }

    private void RefreshWindows()
    {
        _windows.Clear();
        foreach (var window in ScreenRecordingService.GetWindows())
        {
            _windows.Add(window);
        }

        if (_windows.Count > 0)
        {
            WindowComboBox.SelectedIndex = 0;
            UpdateSelectedWindowText();
        }
    }

    private void RefreshMicrophones()
    {
        _microphones.Clear();
        foreach (var microphone in ScreenRecordingService.GetMicrophones())
        {
            _microphones.Add(microphone);
        }

        if (_microphones.Count > 0)
        {
            MicrophoneComboBox.SelectedIndex = 0;
        }
    }

    private void ModeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        UpdateModeUi();
        UpdateSelectedWindowText();
    }

    private void UpdateModeUi()
    {
        var selectedMode = GetSelectedMode();
        var isAudioOnly = selectedMode == CaptureMode.AudioOnly;
        var isWindowMode = selectedMode == CaptureMode.Window;

        UpdateModeSelectionVisuals(selectedMode);
        ApplyModeLayoutCompression(isWindowMode);
        WindowSelectorPanel.Visibility = isWindowMode ? Visibility.Visible : Visibility.Collapsed;
        IncludeSystemAudioCheckBox.IsEnabled = !isAudioOnly;
        IncludeMicrophoneCheckBox.IsEnabled = true;
        IncludeMicrophoneCheckBox.Visibility = isAudioOnly ? Visibility.Collapsed : Visibility.Visible;
        SystemAudioVolumePanel.Visibility = IncludeSystemAudioCheckBox.IsChecked == true && !isAudioOnly
            ? Visibility.Visible
            : Visibility.Collapsed;
        MicrophoneVolumePanel.Visibility = IncludeMicrophoneCheckBox.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        FrameRateComboBox.IsEnabled = !isAudioOnly;
        ResolutionComboBox.IsEnabled = !isAudioOnly;
        BitrateTextBox.IsEnabled = !isAudioOnly;
        StatusTextBlock.Text = isAudioOnly ? T("ModeAudio") : T("ModeVideo");
        UpdateStorageEstimate();
    }

    private void ApplyModeLayoutCompression(bool isWindowMode)
    {
        if (ModeButtonsPanel is null || SettingsCardsGrid is null)
        {
            return;
        }

        var modeButtonHeight = isWindowMode ? 60d : 88d;
        var modeButtonsMarginTop = isWindowMode ? 2d : 8d;
        var selectorMarginTop = isWindowMode ? 4d : 12d;
        var settingsMarginTop = isWindowMode ? 4d : 12d;

        ModeButtonsPanel.Margin = new Thickness(0, modeButtonsMarginTop, 0, 0);
        WindowSelectorPanel.Margin = new Thickness(0, selectorMarginTop, 0, 0);
        SettingsCardsGrid.Margin = new Thickness(0, settingsMarginTop, 0, 0);

        FullScreenModeButton.Height = modeButtonHeight;
        WindowModeButton.Height = modeButtonHeight;
        AudioOnlyModeButton.Height = modeButtonHeight;

        if (HeroCardBorder is not null)
        {
            HeroCardBorder.Padding = isWindowMode
                ? new Thickness(14, 10, 14, 10)
                : new Thickness(16, 14, 16, 14);
        }

        if (BodyContentGrid is not null)
        {
            BodyContentGrid.Margin = isWindowMode
                ? new Thickness(0, 10, 0, 10)
                : new Thickness(0, 14, 0, 14);
        }
    }

    private void UpdateModeSelectionVisuals(CaptureMode selectedMode)
    {
        ApplyModeButtonStyle(FullScreenModeButton, selectedMode == CaptureMode.FullScreen);
        ApplyModeButtonStyle(WindowModeButton, selectedMode == CaptureMode.Window);
        ApplyModeButtonStyle(AudioOnlyModeButton, selectedMode == CaptureMode.AudioOnly);
    }

    private static void ApplyModeButtonStyle(System.Windows.Controls.Button button, bool isSelected)
    {
        button.Background = isSelected
            ? System.Windows.Media.Brushes.White
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 251, 255));
        button.BorderBrush = isSelected
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(221, 228, 239));
        button.Foreground = isSelected
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 78, 175))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 64, 86));
    }

    private CaptureMode GetSelectedMode()
    {
        return ModeComboBox.SelectedItem is CaptureModeOption option ? option.Mode : CaptureMode.FullScreen;
    }

    private void FullScreenModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ModeComboBox.SelectedIndex = 0;
    }

    private void WindowModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ModeComboBox.SelectedIndex = 1;
    }

    private void AudioOnlyModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        ModeComboBox.SelectedIndex = 2;
    }

    private string BuildOutputPath(CaptureMode mode)
    {
        var folder = SaveFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new InvalidOperationException(T("ChooseSaveFolderFirst"));
        }

        Directory.CreateDirectory(folder);

        var extension = mode == CaptureMode.AudioOnly ? "wav" : "mp4";
        var prefix = BuildSanitizedRecordingName(mode);
        return Path.Combine(folder, $"{prefix}-{DateTime.Now:yyMMdd-HHmmss}.{extension}");
    }

    private string BuildSanitizedRecordingName(CaptureMode mode)
    {
        var raw = RecordingNameComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = "X";
        }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c, '-');
        }

        return raw.Trim().Trim('.');
    }

    private ResolutionOption GetSelectedResolution()
    {
        return ResolutionComboBox.SelectedItem as ResolutionOption
               ?? new ResolutionOption { Label = "1080P (1920 x 1080)", Width = 1920, Height = 1080 };
    }

    private int GetRecommendedBitrate(ResolutionOption resolution)
    {
        var baseBitrate = (resolution.Width, resolution.Height) switch
        {
            (2560, 1440) => 12000,
            (1920, 1080) => 6000,
            (1280, 720) => 3000,
            (854, 480) => 1500,
            _ => 6000
        };

        var frameRate = FrameRateComboBox.SelectedItem is int selectedFrameRate ? selectedFrameRate : 60;

        return (int)Math.Round(baseBitrate * (frameRate / 60d));
    }

    private void ApplyRecommendedBitrateForResolution()
    {
        var recommendedBitrate = GetRecommendedBitrate(GetSelectedResolution());
        if (!_hasAppliedInitialBitrateRecommendation ||
            !int.TryParse(BitrateTextBox.Text, out var currentBitrate) ||
            currentBitrate <= 0 ||
            currentBitrate == _lastRecommendedBitrate)
        {
            _isUpdatingBitrateRecommendation = true;
            BitrateTextBox.Text = recommendedBitrate.ToString();
            _isUpdatingBitrateRecommendation = false;
        }

        _hasAppliedInitialBitrateRecommendation = true;
        _lastRecommendedBitrate = recommendedBitrate;
        BitrateHintTextBlock.Text = string.Format(T("BitrateHint"), recommendedBitrate);
        UpdateStorageEstimate();
    }

    private void UpdateStorageEstimate()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (GetSelectedMode() == CaptureMode.AudioOnly)
        {
            StorageEstimateTextBlock.Text = T("StorageEstimateAudioOnly");
            return;
        }

        var folder = SaveFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(folder))
        {
            StorageEstimateTextBlock.Text = T("StorageEstimateUnavailable");
            return;
        }

        if (!int.TryParse(BitrateTextBox.Text, out var bitrateKbps) || bitrateKbps <= 0)
        {
            StorageEstimateTextBlock.Text = T("StorageEstimateInvalidBitrate");
            return;
        }

        try
        {
            var root = Path.GetPathRoot(folder);
            if (string.IsNullOrWhiteSpace(root))
            {
                StorageEstimateTextBlock.Text = T("StorageEstimateUnavailable");
                return;
            }

            var driveInfo = new DriveInfo(root);
            var frameRate = FrameRateComboBox.SelectedItem is int selectedFrameRate ? selectedFrameRate : 60;
            var effectiveBitrateKbps = bitrateKbps * (frameRate / 60d);
            var bytesPerMinute = effectiveBitrateKbps * 1000d / 8d * 60d;
            if (bytesPerMinute <= 0)
            {
                StorageEstimateTextBlock.Text = T("StorageEstimateUnavailable");
                return;
            }

            var minutesAvailable = driveInfo.AvailableFreeSpace / bytesPerMinute;
            StorageEstimateTextBlock.Text = string.Format(
                T("StorageEstimate"),
                FormatBytes(bytesPerMinute),
                FormatDuration(minutesAvailable),
                root.TrimEnd('\\'),
                FormatBytes(driveInfo.AvailableFreeSpace));
        }
        catch
        {
            StorageEstimateTextBlock.Text = T("StorageEstimateUnavailable");
        }
    }

    private static string FormatBytes(double bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        while (bytes >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }

        return $"{bytes:0.#} {suffixes[order]}";
    }

    private static string FormatDuration(double minutes)
    {
        if (double.IsNaN(minutes) || double.IsInfinity(minutes) || minutes <= 0)
        {
            return "0m";
        }

        var totalMinutes = (int)Math.Floor(minutes);
        var hours = totalMinutes / 60;
        var remainingMinutes = totalMinutes % 60;

        if (hours <= 0)
        {
            return $"{remainingMinutes}m";
        }

        return $"{hours}h {remainingMinutes}m";
    }

    private RecordingRequest BuildRequest()
    {
        var mode = GetSelectedMode();

        if (mode != CaptureMode.AudioOnly &&
            IncludeSystemAudioCheckBox.IsChecked != true &&
            IncludeMicrophoneCheckBox.IsChecked != true)
        {
            throw new InvalidOperationException(T("ChooseAudioSource"));
        }

        if (mode != CaptureMode.AudioOnly && MicrophoneComboBox.SelectedItem is null)
        {
            // video recording without microphone is allowed
        }
        else if (mode == CaptureMode.AudioOnly && IncludeMicrophoneCheckBox.IsChecked == true && MicrophoneComboBox.SelectedItem is not MicrophoneItem)
        {
            throw new InvalidOperationException(T("NoMicrophone"));
        }

        var resolution = GetSelectedResolution();
        var request = new RecordingRequest
        {
            Mode = mode,
            OutputPath = BuildOutputPath(mode),
            Microphone = MicrophoneComboBox.SelectedItem as MicrophoneItem,
            IncludeSystemAudio = IncludeSystemAudioCheckBox.IsChecked == true,
            IncludeMicrophone = IncludeMicrophoneCheckBox.IsChecked == true,
            SystemAudioVolume = (int)Math.Round(SystemAudioSlider.Value),
            MicrophoneVolume = (int)Math.Round(MicrophoneSlider.Value),
            MicrophoneBoostPercent = _microphoneBoostPercent,
            FrameRate = FrameRateComboBox.SelectedItem is int frameRate ? frameRate : 60,
            VideoBitrateKbps = int.TryParse(BitrateTextBox.Text, out var bitrate) ? bitrate : 12000,
            OutputWidth = resolution.Width,
            OutputHeight = resolution.Height
        };

        if (mode == CaptureMode.Window)
        {
            request.Window = WindowComboBox.SelectedItem as CaptureWindowItem;
            if (request.Window is null)
            {
                throw new InvalidOperationException(T("ChooseWindow"));
            }
        }

        return request;
    }

    private async void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveRecordingNameState(pushToHistory: true);
            var request = BuildRequest();
            SetIsRecording(true);
            await RunCountdownAsync();

            if (request.Mode == CaptureMode.AudioOnly)
            {
                if (request.Microphone is null)
                {
                    throw new InvalidOperationException(T("NoMicrophone"));
                }

                _audioOnlyRecorder.Start(request.OutputPath, request.Microphone.WaveInDeviceNumber);
                StatusTextBlock.Text = string.Format(T("RecordingAudio"), request.OutputPath);
            }
            else
            {
                _screenRecordingService.Start(request);
                StatusTextBlock.Text = string.Format(T("RecordingVideo"), request.OutputPath);
            }

            BeginRecordingSession(request.Mode);
        }
        catch (Exception ex)
        {
            await StopActiveRecordingAsync();
            SetIsRecording(false);
            MessageBox.Show(this, ex.Message, T("StartFailed"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = T("StartFailed");
        }
    }

    private async Task StopActiveRecordingAsync()
    {
        EndRecordingSession();

        if (_screenRecordingService.IsRecording)
        {
            await _screenRecordingService.StopAsync();
        }

        if (_audioOnlyRecorder.IsRecording)
        {
            await _audioOnlyRecorder.StopAsync();
        }

        StatusTextBlock.Text = T("Stopped");
    }

    private void SetIsRecording(bool isRecording)
    {
        StartButton.IsEnabled = !isRecording;
        ModeComboBox.IsEnabled = !isRecording;
        WindowComboBox.IsEnabled = !isRecording;
        MicrophoneComboBox.IsEnabled = !isRecording;
        IncludeSystemAudioCheckBox.IsEnabled = !isRecording && GetSelectedMode() != CaptureMode.AudioOnly;
        ChooseFolderButton.IsEnabled = !isRecording;
        SaveFolderTextBox.IsEnabled = !isRecording;
        ResolutionComboBox.IsEnabled = !isRecording && GetSelectedMode() != CaptureMode.AudioOnly;
    }

    private async Task RunCountdownAsync()
    {
        var countdownWindow = new CountdownWindow(_language);
        countdownWindow.Show();

        for (var seconds = 5; seconds >= 1; seconds--)
        {
            countdownWindow.SetSeconds(seconds);
            await Task.Delay(1000);
        }

        countdownWindow.Close();
    }

    private void BeginRecordingSession(CaptureMode mode)
    {
        _pausedElapsed = TimeSpan.Zero;
        _recordingStopwatch.Restart();
        _elapsedTimer.Start();

        Hide();

        _recordingControlWindow = new RecordingControlWindow(_language);
        _recordingControlWindow.SetPauseEnabled(mode != CaptureMode.AudioOnly);
        _recordingControlWindow.SetPaused(false);
        _recordingControlWindow.SetElapsed(TimeSpan.Zero);
        _recordingControlWindow.SetSystemAudioEnabled(IncludeSystemAudioCheckBox.IsChecked == true);
        _recordingControlWindow.SetMicrophoneEnabled(IncludeMicrophoneCheckBox.IsChecked == true);
        _recordingControlWindow.SetSystemAudioVolume((int)Math.Round(SystemAudioSlider.Value));
        _recordingControlWindow.SetMicrophoneVolume((int)Math.Round(MicrophoneSlider.Value));
        _recordingControlWindow.SetMicrophoneBoostPercent(_microphoneBoostPercent);
        _recordingControlWindow.PauseResumeRequested += OnPauseResumeRequested;
        _recordingControlWindow.StopRequested += OnStopRequested;
        _recordingControlWindow.SystemAudioVolumeChanged += OnControlWindowSystemAudioVolumeChanged;
        _recordingControlWindow.MicrophoneVolumeChanged += OnControlWindowMicrophoneVolumeChanged;
        _recordingControlWindow.MicrophoneBoostRequested += OnMicrophoneBoostRequested;
        _recordingControlWindow.Show();

    }

    private void EndRecordingSession()
    {
        _elapsedTimer.Stop();
        _recordingStopwatch.Reset();
        _pausedElapsed = TimeSpan.Zero;

        if (_recordingControlWindow is not null)
        {
            _recordingControlWindow.AllowClose();
            _recordingControlWindow.PauseResumeRequested -= OnPauseResumeRequested;
            _recordingControlWindow.StopRequested -= OnStopRequested;
            _recordingControlWindow.SystemAudioVolumeChanged -= OnControlWindowSystemAudioVolumeChanged;
            _recordingControlWindow.MicrophoneVolumeChanged -= OnControlWindowMicrophoneVolumeChanged;
            _recordingControlWindow.MicrophoneBoostRequested -= OnMicrophoneBoostRequested;
            _recordingControlWindow.Close();
            _recordingControlWindow = null;
        }

        if (_isClosing)
        {
            return;
        }

        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
    }

    private void UpdateElapsedUi()
    {
        var elapsed = _pausedElapsed + (_screenRecordingService.IsPaused ? TimeSpan.Zero : _recordingStopwatch.Elapsed);
        _recordingControlWindow?.SetElapsed(elapsed);
    }

    private async void OnPauseResumeRequested()
    {
        if (!_screenRecordingService.IsRecording)
        {
            return;
        }

        if (_screenRecordingService.IsPaused)
        {
            _recordingStopwatch.Restart();
            await _screenRecordingService.ResumeAsync();
            _recordingControlWindow?.SetPaused(false);
        }
        else
        {
            _pausedElapsed += _recordingStopwatch.Elapsed;
            _recordingStopwatch.Reset();
            await _screenRecordingService.PauseAsync();
            _recordingControlWindow?.SetPaused(true);
        }

        UpdateElapsedUi();
    }

    private async void OnStopRequested()
    {
        await StopActiveRecordingAsync();
        SetIsRecording(false);
    }

    private void IncludeSystemAudioCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateModeUi();
        SyncAudioVolumesToControlWindow();
    }

    private void IncludeMicrophoneCheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        UpdateModeUi();
        SyncAudioVolumesToControlWindow();
    }

    private void SystemAudioSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SystemAudioValueTextBlock is null || MicrophoneSlider is null)
        {
            return;
        }

        var value = (int)Math.Round(SystemAudioSlider.Value);
        SystemAudioValueTextBlock.Text = value.ToString();
        if (!IsLoaded || _isSyncingControlWindow)
        {
            return;
        }

        _screenRecordingService.UpdateVolumes(value, (int)Math.Round(MicrophoneSlider.Value), _microphoneBoostPercent);
        if (_recordingControlWindow is not null)
        {
            _isSyncingControlWindow = true;
            _recordingControlWindow.SetSystemAudioVolume(value);
            _isSyncingControlWindow = false;
        }
    }

    private void MicrophoneSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MicrophoneValueTextBlock is null || SystemAudioSlider is null)
        {
            return;
        }

        var value = (int)Math.Round(MicrophoneSlider.Value);
        MicrophoneValueTextBlock.Text = value.ToString();
        if (!IsLoaded || _isSyncingControlWindow)
        {
            return;
        }

        _screenRecordingService.UpdateVolumes((int)Math.Round(SystemAudioSlider.Value), value, _microphoneBoostPercent);
        if (_recordingControlWindow is not null)
        {
            _isSyncingControlWindow = true;
            _recordingControlWindow.SetMicrophoneVolume(value);
            _isSyncingControlWindow = false;
        }
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
        CycleMicrophoneBoost();
    }

    private void OnControlWindowSystemAudioVolumeChanged(int value)
    {
        _isSyncingControlWindow = true;
        SystemAudioSlider.Value = value;
        _isSyncingControlWindow = false;
        _screenRecordingService.UpdateVolumes(value, (int)Math.Round(MicrophoneSlider.Value), _microphoneBoostPercent);
    }

    private void OnControlWindowMicrophoneVolumeChanged(int value)
    {
        _isSyncingControlWindow = true;
        MicrophoneSlider.Value = value;
        _isSyncingControlWindow = false;
        _screenRecordingService.UpdateVolumes((int)Math.Round(SystemAudioSlider.Value), value, _microphoneBoostPercent);
    }

    private void SyncAudioVolumesToControlWindow()
    {
        if (_recordingControlWindow is null)
        {
            return;
        }

        _recordingControlWindow.SetSystemAudioEnabled(IncludeSystemAudioCheckBox.IsChecked == true);
        _recordingControlWindow.SetMicrophoneEnabled(IncludeMicrophoneCheckBox.IsChecked == true);
        _recordingControlWindow.SetSystemAudioVolume((int)Math.Round(SystemAudioSlider.Value));
        _recordingControlWindow.SetMicrophoneVolume((int)Math.Round(MicrophoneSlider.Value));
        _recordingControlWindow.SetMicrophoneBoostPercent(_microphoneBoostPercent);
    }

    private void RecordingNameComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializingUi)
        {
            return;
        }

        _settings.LastRecordingName = RecordingNameComboBox.Text;
        _settings.PreferredLanguage = _language;
        _settingsService.Save(_settings);
    }

    private void HookRecordingNameEditor()
    {
        if (RecordingNameComboBox.Template.FindName("PART_EditableTextBox", RecordingNameComboBox) is not System.Windows.Controls.TextBox editor)
        {
            RecordingNameComboBox.ApplyTemplate();
            editor = RecordingNameComboBox.Template.FindName("PART_EditableTextBox", RecordingNameComboBox) as System.Windows.Controls.TextBox
                ?? throw new InvalidOperationException("Recording name editor not found.");
        }

        editor.TextChanged -= RecordingNameEditor_OnTextChanged;
        editor.LostKeyboardFocus -= RecordingNameEditor_OnLostKeyboardFocus;
        editor.TextChanged += RecordingNameEditor_OnTextChanged;
        editor.LostKeyboardFocus += RecordingNameEditor_OnLostKeyboardFocus;
    }

    private void RecordingNameEditor_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializingUi)
        {
            return;
        }

        _settings.LastRecordingName = RecordingNameComboBox.Text;
        _settings.PreferredLanguage = _language;
        _settingsService.Save(_settings);

        if (string.IsNullOrWhiteSpace(RecordingNameComboBox.Text))
        {
            RecordingNameComboBox.IsDropDownOpen = _recentRecordingNames.Count > 0;
        }
        else
        {
            RecordingNameComboBox.IsDropDownOpen = false;
        }
    }

    private void RecordingNameEditor_OnLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        SaveRecordingNameState(pushToHistory: true);
    }

    private void SaveRecordingNameState(bool pushToHistory)
    {
        var text = RecordingNameComboBox.Text.Trim();
        _settings.LastRecordingName = text;
        _settings.PreferredLanguage = _language;

        if (pushToHistory && !string.IsNullOrWhiteSpace(text))
        {
            var updated = _settings.RecentRecordingNames
                .Where(name => !string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                .Prepend(text)
                .Take(3)
                .ToList();

            _settings.RecentRecordingNames = updated;
            ReplaceRecordingNameHistory(updated);
        }

        _settingsService.Save(_settings);
    }

    private void ReplaceRecordingNameHistory(IEnumerable<string>? names)
    {
        _recentRecordingNames.Clear();
        foreach (var name in names?.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3)
                     ?? Enumerable.Empty<string>())
        {
            _recentRecordingNames.Add(name);
        }
    }

    private void ResolutionComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyRecommendedBitrateForResolution();
    }

    private void BitrateTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!_isUpdatingBitrateRecommendation)
        {
            BitrateHintTextBlock.Text = string.Format(T("BitrateHint"), GetRecommendedBitrate(GetSelectedResolution()));
        }

        UpdateStorageEstimate();
    }

    private void FrameRateComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyRecommendedBitrateForResolution();
    }

    private void CycleMicrophoneBoost()
    {
        _microphoneBoostPercent = _microphoneBoostPercent >= 500 ? 100 : _microphoneBoostPercent + 100;
        MicrophoneBoostButton.Content = $"x{_microphoneBoostPercent / 100}";
        _recordingControlWindow?.SetMicrophoneBoostPercent(_microphoneBoostPercent);
        _screenRecordingService.UpdateVolumes((int)Math.Round(SystemAudioSlider.Value), (int)Math.Round(MicrophoneSlider.Value), _microphoneBoostPercent);
    }

    private void OnMicrophoneBoostRequested()
    {
        CycleMicrophoneBoost();
    }

    private void RefreshWindowsButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshWindows();
        StatusTextBlock.Text = _windows.Count == 0
            ? T("NoWindowFound")
            : string.Format(T("WindowRefreshed"), _windows.Count);
    }

    private void ChooseWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_windows.Count == 0)
        {
            RefreshWindows();
        }

        if (_windows.Count == 0)
        {
            MessageBox.Show(this, T("NoWindowFound"), T("ChooseWindow"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new WindowSelectionWindow(_windows, WindowComboBox.SelectedItem as CaptureWindowItem, _language)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.SelectedWindow is not null)
        {
            WindowComboBox.SelectedItem = _windows.FirstOrDefault(item => item.Handle == dialog.SelectedWindow.Handle);
            UpdateSelectedWindowText();
        }
    }

    private void UpdateSelectedWindowText()
    {
        SelectedWindowTextBox.Text = WindowComboBox.SelectedItem is CaptureWindowItem item
            ? item.Title
            : T("ChooseWindowPlaceholder");
    }

    private void ChooseFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T("ChooseSaveLocation"),
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_settings.SaveFolderPath)
                ? _settings.SaveFolderPath
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _settings.SaveFolderPath = dialog.SelectedPath;
            _settings.PreferredLanguage = _language;
            _settingsService.Save(_settings);
            SaveFolderTextBox.Text = _settings.SaveFolderPath;
            StatusTextBlock.Text = T("SaveUpdated");
            UpdateStorageEstimate();
        }
    }

    private void OpenSaveFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var folder = SaveFolderTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show(this, T("ChooseSaveFolderFirst"), T("SaveTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = folder,
            UseShellExecute = true
        });
    }

    private void SupportAuthorButton_OnClick(object sender, RoutedEventArgs e)
    {
        var supportWindow = new SupportAuthorWindow(_language)
        {
            Owner = this
        };
        supportWindow.ShowDialog();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        await StopActiveRecordingAsync();
        _audioOnlyRecorder.Dispose();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _isClosing = true;
    }
}
