using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;

namespace FuturisticCtrlHud;

public sealed class QuantityDialog : Window
{
    private readonly TextBlock _value = new();
    private readonly string _itemName;
    private int _quantity = 1;

    public int Quantity => _quantity;

    private QuantityDialog(Window owner, string itemName)
    {
        Owner = owner;
        _itemName = itemName;
        Title = "Quantity";
        Width = 420;
        Height = 300;
        MinWidth = 390;
        MinHeight = 280;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
        KeyDown += OnKeyDown;
        Content = BuildContent();
        UpdateValue();
    }

    public static int Ask(Window owner, string itemName)
    {
        var dialog = new QuantityDialog(owner, itemName);
        return dialog.ShowDialog() == true ? dialog.Quantity : 0;
    }

    private UIElement BuildContent()
    {
        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        header.Children.Add(new TextBlock
        {
            Text = "Select quantity",
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
            TextAlignment = TextAlignment.Center
        });
        header.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xF2, 0xFE)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new TextBlock
            {
                Text = _itemName,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x03, 0x47, 0x66)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 42
            }
        });
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var quantityCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xE0, 0xEA)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.12
            }
        };

        var controls = new Grid();
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        controls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });

        var minus = RoundButton("\u2212", "Decrease quantity", Color.FromRgb(0xFF, 0xED, 0xE8), Color.FromRgb(0xB4, 0x23, 0x18));
        var plus = RoundButton("+", "Increase quantity", Color.FromRgb(0xE8, 0xFA, 0xF0), Color.FromRgb(0x06, 0x7A, 0x46));
        minus.Click += (_, _) => ChangeQuantity(-1);
        plus.Click += (_, _) => ChangeQuantity(1);

        _value.FontSize = 52;
        _value.FontWeight = FontWeights.Black;
        _value.Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
        _value.TextAlignment = TextAlignment.Center;
        _value.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(minus, 0);
        Grid.SetColumn(_value, 1);
        Grid.SetColumn(plus, 2);
        controls.Children.Add(minus);
        controls.Children.Add(_value);
        controls.Children.Add(plus);
        quantityCard.Child = controls;
        Grid.SetRow(quantityCard, 1);
        root.Children.Add(quantityCard);

        var footer = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var cancel = ActionButton("Esc  Cancel", Color.FromRgb(0xFF, 0xEA, 0xEA), Color.FromRgb(0xB4, 0x23, 0x18));
        var submit = ActionButton("Enter  Submit", Color.FromRgb(0xDB, 0xEA, 0xFF), Color.FromRgb(0x1D, 0x4E, 0x89));
        cancel.IsCancel = true;
        submit.IsDefault = true;
        cancel.Click += (_, _) => DialogResult = false;
        submit.Click += (_, _) => DialogResult = true;
        Grid.SetColumn(cancel, 0);
        Grid.SetColumn(submit, 1);
        footer.Children.Add(cancel);
        footer.Children.Add(submit);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return root;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                DialogResult = true;
                break;
            case Key.Escape:
                DialogResult = false;
                break;
            case Key.Add:
            case Key.OemPlus:
            case Key.Up:
            case Key.Right:
                ChangeQuantity(1);
                break;
            case Key.Subtract:
            case Key.OemMinus:
            case Key.Down:
            case Key.Left:
                ChangeQuantity(-1);
                break;
        }
    }

    private void ChangeQuantity(int delta)
    {
        _quantity = System.Math.Max(1, _quantity + delta);
        UpdateValue();
    }

    private void UpdateValue()
    {
        _value.Text = _quantity.ToString(CultureInfo.InvariantCulture);
    }

    private static Button RoundButton(string label, string toolTip, Color background, Color foreground)
    {
        var button = new Button
        {
            Content = label,
            ToolTip = toolTip,
            Width = 78,
            Height = 78,
            Margin = new Thickness(6),
            Background = new SolidColorBrush(background),
            Foreground = new SolidColorBrush(foreground),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 28,
            FontWeight = FontWeights.Black,
            Cursor = Cursors.Hand,
            Template = RoundedTemplate(18)
        };
        return button;
    }

    private static Button ActionButton(string label, Color background, Color foreground)
    {
        var button = new Button
        {
            Content = label,
            Height = 42,
            Margin = new Thickness(5, 0, 5, 0),
            Background = new SolidColorBrush(background),
            Foreground = new SolidColorBrush(foreground),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontWeight = FontWeights.Bold,
            Cursor = Cursors.Hand,
            Template = RoundedTemplate(10)
        };
        return button;
    }

    private static ControlTemplate RoundedTemplate(double radius)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetBinding(Border.BackgroundProperty, new Binding(nameof(Button.Background)) { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Button.BorderBrush)) { RelativeSource = RelativeSource.TemplatedParent });

        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        content.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(Button.Content)) { RelativeSource = RelativeSource.TemplatedParent });
        border.AppendChild(content);

        return new ControlTemplate(typeof(Button)) { VisualTree = border };
    }
}
