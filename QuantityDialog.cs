using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class QuantityDialog : Window
{
    private readonly TextBlock _value = new();
    private int _quantity = 1;

    public int Quantity => _quantity;

    private QuantityDialog(Window owner)
    {
        Owner = owner;
        Title = "Quantity";
        Width = 310;
        Height = 170;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = BuildContent();
        UpdateValue();
    }

    public static int Ask(Window owner)
    {
        var dialog = new QuantityDialog(owner);
        return dialog.ShowDialog() == true ? dialog.Quantity : 0;
    }

    private UIElement BuildContent()
    {
        var root = new StackPanel { Margin = new Thickness(18) };
        root.Children.Add(new TextBlock
        {
            Text = "Select quantity",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var minus = Button("-", "#FFF4DE", 54);
        var plus = Button("+", "#ECFDF5", 54);
        _value.FontSize = 26;
        _value.FontWeight = FontWeights.Bold;
        _value.Width = 72;
        _value.TextAlignment = TextAlignment.Center;
        minus.Click += (_, _) =>
        {
            _quantity = System.Math.Max(1, _quantity - 1);
            UpdateValue();
        };
        plus.Click += (_, _) =>
        {
            _quantity++;
            UpdateValue();
        };
        controls.Children.Add(minus);
        controls.Children.Add(_value);
        controls.Children.Add(plus);
        root.Children.Add(controls);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = Button("Cancel", "#FFEAEA", 88);
        var submit = Button("Submit", "#E8F1FF", 88);
        cancel.Click += (_, _) => DialogResult = false;
        submit.Click += (_, _) => DialogResult = true;
        buttons.Children.Add(cancel);
        buttons.Children.Add(submit);
        root.Children.Add(buttons);
        return root;
    }

    private void UpdateValue()
    {
        _value.Text = _quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Button Button(string label, string color, double width) => new()
    {
        Content = label,
        Width = width,
        Height = 34,
        Margin = new Thickness(6, 0, 0, 0),
        Background = (Brush)new BrushConverter().ConvertFromString(color)!,
        BorderBrush = Brushes.LightGray,
        FontWeight = FontWeights.SemiBold
    };
}
