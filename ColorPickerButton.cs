using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class ColorPickerButton : Button
{
    private string _selectedColorHex = "#26D9FF";

    public string SelectedColorHex
    {
        get => _selectedColorHex;
        set
        {
            _selectedColorHex = NormalizeHex(value);
            UpdateSwatch();
        }
    }

    public ColorPickerButton()
    {
        Width = 86;
        Height = 28;
        ToolTip = "Choose accent color";
        Click += (_, _) =>
        {
            var picker = new ColorPickerWindow(SelectedColorHex)
            {
                Owner = Window.GetWindow(this)
            };
            if (picker.ShowDialog() == true)
            {
                SelectedColorHex = picker.SelectedColorHex;
            }
        };
        UpdateSwatch();
    }

    private void UpdateSwatch()
    {
        var color = ParseColor(_selectedColorHex);
        Background = new SolidColorBrush(color);
        Foreground = GetReadableForeground(color);
        Content = _selectedColorHex;
    }

    private static Brush GetReadableForeground(Color color)
    {
        var brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        return brightness > 145 ? Brushes.Black : Brushes.White;
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return Color.FromRgb(0x26, 0xD9, 0xFF);
        }
    }

    private static string NormalizeHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#26D9FF";
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            trimmed = "#" + trimmed;
        }

        return trimmed.Length == 7 ? trimmed.ToUpperInvariant() : "#26D9FF";
    }
}

public sealed class ColorPickerWindow : Window
{
    private readonly Slider _red = Slider();
    private readonly Slider _green = Slider();
    private readonly Slider _blue = Slider();
    private readonly Border _preview = new() { Height = 44, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 12) };
    private readonly TextBlock _hex = new() { FontFamily = new FontFamily("Consolas"), FontSize = 14, Margin = new Thickness(0, 0, 0, 12) };

    public string SelectedColorHex { get; private set; }

    public ColorPickerWindow(string initialHex)
    {
        SelectedColorHex = initialHex;
        Title = "Choose Accent Color";
        Width = 360;
        Height = 315;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var initial = ParseColor(initialHex);
        _red.Value = initial.R;
        _green.Value = initial.G;
        _blue.Value = initial.B;

        Content = BuildContent();
        UpdatePreview();
    }

    private UIElement BuildContent()
    {
        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(_preview);
        root.Children.Add(_hex);
        root.Children.Add(SliderRow("Red", _red));
        root.Children.Add(SliderRow("Green", _green));
        root.Children.Add(SliderRow("Blue", _blue));

        _red.ValueChanged += (_, _) => UpdatePreview();
        _green.ValueChanged += (_, _) => UpdatePreview();
        _blue.ValueChanged += (_, _) => UpdatePreview();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var cancel = new Button { Content = "Cancel", Width = 82, Height = 32, Margin = new Thickness(8, 0, 0, 0) };
        var ok = new Button { Content = "OK", Width = 82, Height = 32, Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => DialogResult = false;
        ok.Click += (_, _) =>
        {
            SelectedColorHex = CurrentHex();
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        root.Children.Add(buttons);
        return root;
    }

    private void UpdatePreview()
    {
        _preview.Background = new SolidColorBrush(CurrentColor());
        _hex.Text = CurrentHex();
    }

    private Color CurrentColor() => Color.FromRgb((byte)_red.Value, (byte)_green.Value, (byte)_blue.Value);

    private string CurrentHex() => $"#{(byte)_red.Value:X2}{(byte)_green.Value:X2}{(byte)_blue.Value:X2}";

    private static UIElement SliderRow(string label, Slider slider)
    {
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(slider, 1);
        grid.Children.Add(slider);
        var value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        value.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Value")
        {
            Source = slider,
            StringFormat = "F0"
        });
        Grid.SetColumn(value, 2);
        grid.Children.Add(value);
        return grid;
    }

    private static Slider Slider() => new()
    {
        Minimum = 0,
        Maximum = 255,
        TickFrequency = 1,
        IsSnapToTickEnabled = true
    };

    private static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return Color.FromRgb(0x26, 0xD9, 0xFF);
        }
    }
}
