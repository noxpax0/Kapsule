using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class ReceiptPreviewWindow : Window
{
    private readonly Func<FrameworkElement> _contentFactory;
    private readonly Action _printAction;

    public ReceiptPreviewWindow(string title, Func<FrameworkElement> contentFactory, Action printAction)
    {
        _contentFactory = contentFactory;
        _printAction = printAction;
        Title = title;
        Width = 420;
        Height = 640;
        MinWidth = 360;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
    }

    public static bool ShowIfEnabled(PresetSettings presets, string title, Func<FrameworkElement> contentFactory, Action printAction)
    {
        if (!presets.SuperuserMode)
        {
            return false;
        }

        new ReceiptPreviewWindow(title, contentFactory, printAction).Show();
        return true;
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(16) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var cancel = Button("Cancel", "#FFEAEA");
        var test = Button("Test Print", "#F4EEFF", 96);
        var print = Button("Print", "#E8F1FF");
        cancel.Click += (_, _) => Close();
        test.Click += (_, _) => PrintTestStrip();
        print.Click += (_, _) =>
        {
            _printAction();
            Close();
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(test);
        buttons.Children.Add(print);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var previewHost = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF4, 0xF6)),
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14),
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Child = _contentFactory()
                }
            }
        };
        root.Children.Add(previewHost);
        return root;
    }

    private static void PrintTestStrip()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var width = Math.Min(dialog.PrintableAreaWidth, 302);
        var panel = new StackPanel { Width = width, Background = Brushes.White };
        panel.Children.Add(new TextBlock
        {
            Text = "THERMAL TEST PRINT",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8)
        });
        panel.Children.Add(new TextBlock
        {
            Text = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 0, 8, 8)
        });
        panel.Children.Add(new Border { Height = 1, Background = Brushes.Black, Margin = new Thickness(8, 3, 8, 3) });
        panel.Children.Add(new TextBlock { Text = "Width check: |----------------------|", FontSize = 11, TextAlignment = TextAlignment.Center, Margin = new Thickness(8) });
        panel.Children.Add(new TextBlock { Text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789", FontSize = 11, TextAlignment = TextAlignment.Center, Margin = new Thickness(8) });
        panel.Children.Add(new Border { Height = 18 });
        PrintVisual(dialog, panel, "Thermal Test Print");
    }

    public static void PrintVisual(PrintDialog dialog, FrameworkElement visual, string description)
    {
        visual.Measure(new Size(visual.Width > 0 ? visual.Width : dialog.PrintableAreaWidth, double.PositiveInfinity));
        visual.Arrange(new Rect(new Point(0, 0), visual.DesiredSize));
        visual.UpdateLayout();
        dialog.PrintVisual(visual, description);
    }

    private static Button Button(string label, string color, double width = 82) => new()
    {
        Content = label,
        Width = width,
        Height = 32,
        Margin = new Thickness(8, 0, 0, 0),
        Background = (Brush)new BrushConverter().ConvertFromString(color)!,
        BorderBrush = Brushes.LightGray,
        FontWeight = FontWeights.SemiBold
    };
}
