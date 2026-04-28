using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WindosRecorder.Models;
using WindosRecorder.Services;
using MessageBox = System.Windows.MessageBox;

namespace WindosRecorder;

public partial class WindowSelectionWindow : Window
{
    private readonly string _language;

    public WindowSelectionWindow(IEnumerable<CaptureWindowItem> windows, CaptureWindowItem? selectedWindow, string language)
    {
        _language = LanguageCatalog.Normalize(language);
        InitializeComponent();
        WindowListBox.ItemsSource = windows.ToList();
        ApplyLanguage();

        if (selectedWindow is not null)
        {
            var match = WindowListBox.Items.Cast<CaptureWindowItem>()
                .FirstOrDefault(item => item.Handle == selectedWindow.Handle);
            if (match is not null)
            {
                WindowListBox.SelectedItem = match;
                WindowListBox.ScrollIntoView(match);
            }
        }
    }

    public CaptureWindowItem? SelectedWindow => WindowListBox.SelectedItem as CaptureWindowItem;

    private void ApplyLanguage()
    {
        Title = LanguageCatalog.Get(_language, "ChooseWindow");
        TitleTextBlock.Text = LanguageCatalog.Get(_language, "ChooseWindowDialogTitle");
        SubtitleTextBlock.Text = LanguageCatalog.Get(_language, "ChooseWindowDialogSubtitle");
        ConfirmButton.Content = LanguageCatalog.Get(_language, "LanguageConfirm");
        CancelButton.Content = LanguageCatalog.Get(_language, "LanguageCancel");
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedWindow is null)
        {
            MessageBox.Show(this, LanguageCatalog.Get(_language, "ChooseWindow"), Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void WindowListBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ConfirmButton.IsEnabled = SelectedWindow is not null;
    }
}
