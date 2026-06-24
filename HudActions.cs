using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;

namespace FuturisticCtrlHud;

public static class HudActions
{
    private const double StandardPrintPadding = 12;

    public static void Run(string actionKey)
    {
        var settings = AppSettings.Load();
        switch (actionKey)
        {
            case "print_option_1":
                Console.WriteLine("Option 1 selected");
                break;

            case "print_option_2":
                Console.WriteLine("Option 2 selected");
                break;

            case "open_notepad":
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    UseShellExecute = true
                });
                break;

            case "show_notification":
                MessageBox.Show("HUD action triggered", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
                break;

            case "print_logo":
                PrintLogo(settings.Presets);
                break;

            case "print_qr":
                PrintQr(settings.Presets);
                break;

            case "print_email":
                PrintText("Email", settings.Presets.Email);
                break;

            case "print_mobile":
                PrintText("Mobile", settings.Presets.Mobile);
                break;

            case "print_location":
                PrintText("Location", settings.Presets.Location);
                break;

            case "print_contact_card":
                PrintContactCard(settings.Presets);
                break;

            case "print_poyc":
                PrintText("POYC", settings.Presets.PoycText);
                break;

            case "print_dda":
                new DdaSlipWindow(settings).Show();
                break;

            case "open_handover":
                new HandoverWindow(settings).Show();
                break;

            case "open_custom_text":
                new CustomTextWindow(settings).Show();
                break;

            case "open_order_list":
                new OrderListWindow(settings).Show();
                break;

            case "open_remedy_recipes":
                new RemedyRecipesWindow(settings).Show();
                break;

            case "open_settings":
                new SettingsWindow(settings).Show();
                break;

            default:
                Console.WriteLine($"Unknown HUD action: {actionKey}");
                break;
        }
    }

    private static void PrintLogo(PresetSettings presets)
    {
        var logoPath = AppSettings.ResolveLogoPath(presets);
        if (string.IsNullOrWhiteSpace(logoPath) || !File.Exists(logoPath))
        {
            MessageBox.Show("Choose a logo in Settings first.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var image = new Image
        {
            Source = new BitmapImage(new Uri(logoPath)),
            Stretch = Stretch.Uniform,
            MaxWidth = 300,
            MaxHeight = 300,
            Margin = new Thickness(8)
        };
        PrintVisual("Logo", WrapForPrint(image));
    }

    private static void PrintQr(PresetSettings presets)
    {
        PrintVisual("QR Code", WrapForPrint(CreateQrImage(presets)));
    }

    private static void PrintText(string title, string text, bool justify = false)
    {
        var block = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(text) ? $"No {title.ToLowerInvariant()} configured." : text,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = justify ? 14 : 18,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = justify ? TextAlignment.Justify : TextAlignment.Center,
            Margin = new Thickness(10)
        };
        PrintVisual(title, WrapForPrint(block));
    }

    private static void PrintContactCard(PresetSettings presets)
    {
        if (ReceiptPreviewWindow.ShowIfEnabled(
                presets,
                "Doctor Contacts Preview",
                () => BuildThermalPatientSlip(presets, 302),
                () => PrintContactCardDirect(presets)))
        {
            return;
        }

        PrintContactCardDirect(presets);
    }

    private static void PrintContactCardDirect(PresetSettings presets)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var slipWidth = Math.Min(dialog.PrintableAreaWidth, 302);
        var panel = BuildThermalPatientSlip(presets, slipWidth);
        ReceiptPreviewWindow.PrintVisual(dialog, panel, "Patient QR Contact Slip");
    }

    private static FrameworkElement BuildThermalPatientSlip(PresetSettings presets, double slipWidth)
    {
        var website = string.IsNullOrWhiteSpace(presets.QrWebsite) ? "https://example.com" : presets.QrWebsite.Trim();
        var panel = new StackPanel
        {
            Width = slipWidth,
            Background = Brushes.White,
            Margin = new Thickness(0),
            Orientation = Orientation.Vertical
        };

        var row = new Grid
        {
            Width = slipWidth,
            Margin = new Thickness(8, 8, 8, 8)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var logoBox = new Border
        {
            Height = 116,
            Margin = new Thickness(0, 0, 8, 0),
            Child = CreateLogoForSlip(presets)
        };
        Grid.SetColumn(logoBox, 0);
        row.Children.Add(logoBox);

        var qrImage = new Image
        {
            Source = CreateQrBitmap(website, 8),
            Width = 116,
            Height = 116,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(qrImage, 1);
        row.Children.Add(qrImage);
        panel.Children.Add(row);

        panel.Children.Add(SlipLine($"\U0001F517 {website}", 14, FontWeights.SemiBold, 6));
        panel.Children.Add(SlipLine($"\U0001F4E7 {presets.Email}", 13, FontWeights.Normal, 4));
        panel.Children.Add(SlipLine($"\U0001F4F2 {presets.Mobile}", 13, FontWeights.Normal, 4));
        panel.Children.Add(SlipLine($"\U0001F4CC {presets.Location}", 13, FontWeights.Normal, 4));
        panel.Children.Add(new Border { Height = 10 });
        return panel;
    }

    private static UIElement CreateLogoForSlip(PresetSettings presets)
    {
        var logoPath = AppSettings.ResolveLogoPath(presets);
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            return new Image
            {
                Source = new BitmapImage(new Uri(logoPath)),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return new TextBlock
        {
            Text = "LOGO",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private static TextBlock SlipLine(string value, double fontSize, FontWeight weight, double topMargin) => new()
    {
        Text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim(),
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = fontSize,
        FontWeight = weight,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(10, topMargin, 10, 0)
    };

    private static BitmapImage CreateQrBitmap(string content, int pixelsPerModule)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var bytes = qr.GetGraphic(pixelsPerModule);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static FrameworkElement CreateQrImage(PresetSettings presets)
    {
        var website = string.IsNullOrWhiteSpace(presets.QrWebsite) ? "https://example.com" : presets.QrWebsite.Trim();
        var caption = string.IsNullOrWhiteSpace(presets.QrCaption) ? website : presets.QrCaption.Trim();
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(12)
        };
        panel.Children.Add(new Image
        {
            Source = CreateQrBitmap(website, 10),
            Width = 220,
            Height = 220,
            Stretch = Stretch.Uniform
        });
        panel.Children.Add(new TextBlock
        {
            Text = caption,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
            MaxWidth = 300
        });
        return panel;
    }

    private static TextBlock ContactLine(string label, string value) => new()
    {
        Text = $"{label}: {value}",
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 22,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 10, 0, 0)
    };

    private static Border WrapForPrint(UIElement child) => new()
    {
        Child = child,
        Background = Brushes.White,
        Padding = new Thickness(StandardPrintPadding)
    };

    private static void PrintVisual(string description, FrameworkElement visual)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ReceiptPreviewWindow.PrintVisual(dialog, visual, description);
    }
}
