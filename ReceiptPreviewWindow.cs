using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
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
        var width = Math.Max(1, Math.Min(visual.Width > 0 ? visual.Width : dialog.PrintableAreaWidth, dialog.PrintableAreaWidth));
        var height = Math.Max(1, dialog.PrintableAreaHeight);
        visual.Width = width;
        visual.Measure(new Size(width, double.PositiveInfinity));
        visual.Arrange(new Rect(new Point(0, 0), visual.DesiredSize));
        visual.UpdateLayout();

        if (visual.DesiredSize.Height <= height || IsLikelyRollPrinter(dialog))
        {
            dialog.PrintVisual(visual, description);
            return;
        }

        if (visual is TextBlock textBlock)
        {
            PrintTextBlockDocument(dialog, textBlock, description, width, height);
            return;
        }

        if (visual is Border { Child: TextBlock borderedText })
        {
            PrintTextBlockDocument(dialog, borderedText, description, width, height);
            return;
        }

        if (visual is Border { Child: StackPanel borderedStack })
        {
            PrintStackPanelDocument(dialog, borderedStack, description, width, height);
            return;
        }

        if (visual is StackPanel stackPanel)
        {
            PrintStackPanelDocument(dialog, stackPanel, description, width, height);
            return;
        }

        dialog.PrintVisual(visual, description);
    }

    private static void PrintTextBlockDocument(PrintDialog dialog, TextBlock source, string description, double width, double height)
    {
        var document = new FlowDocument
        {
            PageWidth = width,
            PageHeight = height,
            PagePadding = source.Padding,
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            FontWeight = source.FontWeight,
            TextAlignment = source.TextAlignment,
            ColumnWidth = width
        };
        document.Blocks.Add(new Paragraph(new Run(source.Text))
        {
            Margin = source.Margin
        });
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, description);
    }

    private static void PrintStackPanelDocument(PrintDialog dialog, StackPanel source, string description, double width, double height)
    {
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(width, height);
        var pageNumber = 0;
        var current = NewPrintPage(width, source.Background);
        var currentHeight = 0.0;

        while (source.Children.Count > 0)
        {
            var child = source.Children[0];
            source.Children.RemoveAt(0);
            MeasureElement(child, width);
            var childHeight = Math.Max(1, child.DesiredSize.Height);

            if (current.Children.Count > 0 && currentHeight + childHeight > height)
            {
                AddPage(document, current, width, height, ++pageNumber);
                current = NewPrintPage(width, source.Background);
                currentHeight = 0;
            }

            current.Children.Add(child);
            currentHeight += childHeight;
        }

        if (current.Children.Count > 0)
        {
            AddPage(document, current, width, height, ++pageNumber);
        }

        dialog.PrintDocument(document.DocumentPaginator, description);
    }

    private static StackPanel NewPrintPage(double width, Brush background) => new()
    {
        Width = width,
        Background = background ?? Brushes.White
    };

    private static void AddPage(FixedDocument document, StackPanel content, double width, double height, int pageNumber)
    {
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(new Point(0, 0), new Size(width, Math.Min(height, content.DesiredSize.Height))));
        content.UpdateLayout();

        var page = new FixedPage
        {
            Width = width,
            Height = height,
            Background = Brushes.White
        };
        page.Children.Add(content);
        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(page);
        document.Pages.Add(pageContent);
    }

    private static void MeasureElement(UIElement element, double width)
    {
        element.Measure(new Size(width, double.PositiveInfinity));
        element.Arrange(new Rect(new Point(0, 0), element.DesiredSize));
        element.UpdateLayout();
    }

    private static bool IsLikelyRollPrinter(PrintDialog dialog)
    {
        var name = dialog.PrintQueue?.FullName ?? "";
        return dialog.PrintableAreaHeight > 1600 ||
               name.Contains("thermal", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("pos", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("label", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("zebra", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("bixolon", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("star", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("epson", StringComparison.OrdinalIgnoreCase);
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
