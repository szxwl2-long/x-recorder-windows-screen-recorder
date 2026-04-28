using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindosRecorder.Services;

namespace WindosRecorder;

public partial class SupportAuthorWindow : Window
{
    private readonly string _language;

    public SupportAuthorWindow(string language)
    {
        _language = LanguageCatalog.Normalize(language);
        InitializeComponent();
        ApplyLanguage();
    }

    private string T(string key)
    {
        return LanguageCatalog.Get(_language, key);
    }

    private void ApplyLanguage()
    {
        Title = T("SupportAuthorTitle");
        TitleTextBlock.Text = T("SupportAuthorTitle");
        ApplySupportContent();
        CloseButton.Content = T("LanguageConfirm");
    }

    private void ApplySupportContent()
    {
        if (_language == LanguageCatalog.English)
        {
            Width = 460;
            Height = 500;
            MessageTextBlock.Text = "Scan the PayPal QR code below to support long-X.";
            PrimarySupportTitleTextBlock.Text = "PayPal";
            PrimarySupportCard.SetValue(Grid.ColumnSpanProperty, 3);
            ShowQrCode(PrimarySupportImage, PrimarySupportPlaceholderTextBlock, "Assets/paypal-qr.jpg", null);
            PrimarySupportImage.Margin = new Thickness(58, 0, 58, 42);
            SecondarySupportCard.Visibility = Visibility.Collapsed;
            return;
        }

        Width = 520;
        Height = 500;
        MessageTextBlock.Text = "如果你觉得 X 录屏器好用，欢迎通过下面的微信或支付宝二维码支持 long-X。";
        PrimarySupportTitleTextBlock.Text = "微信";
        SecondarySupportTitleTextBlock.Text = "支付宝";
        PrimarySupportCard.SetValue(Grid.ColumnSpanProperty, 1);
        SecondarySupportCard.SetValue(Grid.ColumnProperty, 2);
        ShowQrCode(PrimarySupportImage, PrimarySupportPlaceholderTextBlock, "Assets/wechat-qr.png", null);
        ShowQrCode(SecondarySupportImage, SecondarySupportPlaceholderTextBlock, "Assets/alipay-qr.jpg", null);
        PrimarySupportImage.Margin = new Thickness(10, 0, 10, 12);
        SecondarySupportImage.Margin = new Thickness(10, 0, 10, 12);
        SecondarySupportCard.Visibility = Visibility.Visible;
    }

    private static void ShowQrCode(System.Windows.Controls.Image image, TextBlock placeholder, string? relativeAssetPath, string? fallbackText)
    {
        if (!string.IsNullOrWhiteSpace(relativeAssetPath))
        {
            image.Source = new BitmapImage(new Uri(relativeAssetPath, UriKind.Relative));
            image.Visibility = Visibility.Visible;
            placeholder.Visibility = Visibility.Collapsed;
            return;
        }

        image.Source = null;
        image.Visibility = Visibility.Collapsed;
        placeholder.Text = fallbackText ?? string.Empty;
        placeholder.Visibility = Visibility.Visible;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
