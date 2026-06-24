using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace FuturisticCtrlHud;

public sealed class CustomTextWindow : Window, IAutoSaveWindow
{
    private readonly AppSettings _settings;
    private readonly TextBox _path = new();
    private readonly TextBox _text = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 14,
        MinHeight = 300
    };

    public CustomTextWindow(AppSettings settings)
    {
        _settings = settings;
        Title = "Custom TxT";
        Width = 620;
        Height = 600;
        MinWidth = 520;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
        LoadFromSettings();
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var close = Button("Close", "#FFEAEA");
        var save = Button("Save", "#E8F1FF");
        var print = Button("Print", "#FFF4DE");
        close.Click += (_, _) => Close();
        save.Click += (_, _) => Save(showMessage: true);
        print.Click += (_, _) => Print();
        buttons.Children.Add(close);
        buttons.Children.Add(save);
        buttons.Children.Add(print);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = "Custom TxT/PDF", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        content.Children.Add(FileRow());
        content.Children.Add(new TextBlock { Text = "TXT content can be edited before printing. PDF files print via the default PDF app.", Opacity = 0.75, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        content.Children.Add(_text);
        root.Children.Add(content);
        return root;
    }

    private UIElement FileRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.Children.Add(_path);
        var browse = Button("Browse", "#ECFDF5");
        browse.Click += (_, _) =>
        {
            var dialog = new OpenFileDialog { Filter = "Text/PDF files|*.txt;*.pdf|Text files|*.txt|PDF files|*.pdf|All files|*.*" };
            if (dialog.ShowDialog() == true)
            {
                _path.Text = dialog.FileName;
                LoadFileIntoEditor();
            }
        };
        Grid.SetColumn(browse, 1);
        grid.Children.Add(browse);
        var load = Button("Load", "#F4EEFF");
        load.Click += (_, _) => LoadFileIntoEditor();
        Grid.SetColumn(load, 2);
        grid.Children.Add(load);
        return grid;
    }

    private void LoadFromSettings()
    {
        _path.Text = _settings.Presets.CustomTextFilePath;
        _text.Text = _settings.Presets.CustomTextContent;
        if (string.IsNullOrWhiteSpace(_text.Text))
        {
            LoadFileIntoEditor(showErrors: false);
        }
    }

    private void LoadFileIntoEditor(bool showErrors = true)
    {
        var file = _path.Text.Trim();
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            return;
        }

        if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            _text.Text = File.ReadAllText(file);
        }
        else if (showErrors && Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("PDF selected. Use Print to send it through the default PDF app.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Save(bool showMessage)
    {
        _settings.Presets.CustomTextFilePath = _path.Text.Trim();
        _settings.Presets.CustomTextContent = _text.Text;
        _settings.Save();
        if (showMessage)
        {
            MessageBox.Show("Custom TxT saved.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void SaveState()
    {
        Save(showMessage: false);
    }

    private void Print()
    {
        Save(showMessage: false);
        var file = _path.Text.Trim();
        if (File.Exists(file) && Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo { FileName = file, Verb = "print", UseShellExecute = true });
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (dialog.PrintQueue.FullName.Contains("PDF", StringComparison.OrdinalIgnoreCase) ||
            dialog.PrintQueue.FullName.Contains("XPS", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "The selected printer looks like a PDF/XPS virtual printer. Choose the physical thermal/paper printer to print on paper.",
                "Futuristic HUD",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var text = new TextBlock
        {
            Text = _text.Text,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Justify,
            Width = dialog.PrintableAreaWidth,
            Padding = new Thickness(12),
            Background = Brushes.White
        };
        ReceiptPreviewWindow.PrintVisual(dialog, text, "Custom TxT");
    }

    private static Button Button(string label, string color) => new()
    {
        Content = label,
        Width = 82,
        Height = 30,
        Margin = new Thickness(8, 0, 0, 0),
        Background = (Brush)new BrushConverter().ConvertFromString(color)!,
        BorderBrush = Brushes.LightGray
    };
}
