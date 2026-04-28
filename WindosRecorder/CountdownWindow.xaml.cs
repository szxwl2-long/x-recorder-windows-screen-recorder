using System.Windows;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class CountdownWindow : Window
{
    private const string CountdownMicHintChinese = "麦克风录制音量过小时请加大倍量或靠近麦克风";
    private const string CountdownMicHintEnglish = "If microphone recording volume is too low, increase the boost or move closer to the microphone.";

    public CountdownWindow(string language)
    {
        InitializeComponent();
        TitleTextBlock.Text = LanguageCatalog.Get(language, "CountdownTitle");
        HintTextBlock.Text = language == LanguageCatalog.English
            ? CountdownMicHintEnglish
            : CountdownMicHintChinese;
        Loaded += (_, _) => CaptureProtection.ExcludeFromCapture(this);
    }

    public void SetSeconds(int seconds)
    {
        SecondsTextBlock.Text = seconds.ToString();
    }
}
