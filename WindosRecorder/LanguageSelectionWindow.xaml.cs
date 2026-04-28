using System.Windows;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class LanguageSelectionWindow : Window
{
    public string SelectedLanguage { get; private set; } = LanguageCatalog.Chinese;

    public LanguageSelectionWindow()
    {
        InitializeComponent();
    }

    private void SelectLanguage(string language)
    {
        SelectedLanguage = language;
        DialogResult = true;
        Close();
    }

    private void ChineseButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectLanguage(LanguageCatalog.Chinese);
    }

    private void EnglishButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectLanguage(LanguageCatalog.English);
    }
}
