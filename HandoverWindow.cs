using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class HandoverWindow : Window, IAutoSaveWindow
{
    private const string HandoverIcon = "\u270D";
    private const string SaveIcon = "\U0001F4BE";
    private const string ClearIcon = "\U0001F9F9";
    private const string PrintIcon = "\U0001F5A8";
    private const string CloseIcon = "\u2716";

    private readonly AppSettings _settings;
    private readonly TextBox _notes = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = 14,
        MinHeight = 260
    };

    public HandoverWindow(AppSettings settings)
    {
        _settings = settings;
        Title = "Handover";
        Width = 600;
        Height = 520;
        MinWidth = 500;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
        _notes.Text = _settings.Presets.HandoverNotes;
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(18) };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var close = StyledButton($"{CloseIcon} Close", Color.FromRgb(0xFF, 0xEA, 0xEA));
        var clear = StyledButton($"{ClearIcon} Clear", Color.FromRgb(0xEC, 0xFD, 0xF5));
        var save = StyledButton($"{SaveIcon} Save", Color.FromRgb(0xE8, 0xF1, 0xFF));
        var print = StyledButton($"{PrintIcon} Print", Color.FromRgb(0xFF, 0xF4, 0xDE));
        close.Click += (_, _) => Close();
        clear.Click += (_, _) => ClearNotes();
        save.Click += (_, _) => SaveDraft(showConfirmation: true);
        print.Click += (_, _) => PrintHandover();
        buttons.Children.Add(close);
        buttons.Children.Add(clear);
        buttons.Children.Add(save);
        buttons.Children.Add(print);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = $"{HandoverIcon} Handover {DateText()}",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });
        content.Children.Add(new TextBlock
        {
            Text = "Enter one note per line. Lines are printed as bullets on the handover slip.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.75,
            Margin = new Thickness(0, 0, 0, 10)
        });
        content.Children.Add(_notes);
        root.Children.Add(content);
        return root;
    }

    private void SaveDraft(bool showConfirmation)
    {
        _settings.Presets.HandoverNotes = _notes.Text;
        _settings.Save();
        if (showConfirmation)
        {
            MessageBox.Show("Handover draft saved.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void SaveState()
    {
        SaveDraft(showConfirmation: false);
    }

    private void ClearNotes()
    {
        if (!string.IsNullOrWhiteSpace(_notes.Text))
        {
            var result = MessageBox.Show(
                "Clear all handover notes?",
                "Futuristic HUD",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _notes.Clear();
        SaveDraft(showConfirmation: false);
    }

    private void PrintHandover()
    {
        SaveDraft(showConfirmation: false);
        if (ReceiptPreviewWindow.ShowIfEnabled(
                _settings.Presets,
                "Handover Preview",
                () => BuildReceiptVisual(302),
                PrintHandoverDirect))
        {
            return;
        }

        PrintHandoverDirect();
    }

    private void PrintHandoverDirect()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var visual = BuildReceiptVisual(Math.Min(dialog.PrintableAreaWidth, 302));
        ReceiptPreviewWindow.PrintVisual(dialog, visual, $"Handover {DateText()}");
    }

    private FrameworkElement BuildReceiptVisual(double width)
    {
        var panel = new StackPanel
        {
            Width = width,
            Background = Brushes.White,
            Margin = new Thickness(0)
        };

        panel.Children.Add(new TextBlock
        {
            Text = $"{HandoverIcon} Handover {DateText()}",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 8, 8, 10)
        });

        var lines = _notes.Text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(CleanBulletLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            lines.Add("No handover notes entered.");
        }

        foreach (var line in lines)
        {
            var row = new Grid { Margin = new Thickness(10, 0, 10, 6) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(new TextBlock { Text = "\u2022", FontSize = 13, FontWeight = FontWeights.Bold });
            var note = new TextBlock
            {
                Text = line,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Justify,
            };
            Grid.SetColumn(note, 1);
            row.Children.Add(note);
            panel.Children.Add(row);
        }

        panel.Children.Add(new Border { Height = 8 });
        return panel;
    }

    private static string CleanBulletLine(string line)
    {
        var trimmed = line.Trim();
        while (trimmed.StartsWith("-", StringComparison.Ordinal) ||
               trimmed.StartsWith("*", StringComparison.Ordinal) ||
               trimmed.StartsWith("\u2022", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..].TrimStart();
        }

        return trimmed;
    }

    private static string DateText() => DateTime.Now.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

    private static Button StyledButton(string label, Color background) => new()
    {
        Content = label,
        Width = 104,
        Height = 34,
        Margin = new Thickness(8, 0, 0, 0),
        Background = new SolidColorBrush(background),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD7, 0xDE)),
        FontWeight = FontWeights.SemiBold
    };
}
