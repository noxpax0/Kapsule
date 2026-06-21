using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FuturisticCtrlHud;

public sealed class SettingsWindow : Window, IAutoSaveWindow
{
    private readonly AppSettings _settings;
    private readonly List<TextBox> _labelBoxes = [];
    private readonly List<ComboBox> _actionBoxes = [];
    private readonly List<ColorPickerButton> _accentButtons = [];
    private bool _loading;
    private readonly TextBox _logoPath = new();
    private readonly TextBox _qrWebsite = new();
    private readonly TextBox _qrCaption = new();
    private readonly TextBox _email = new();
    private readonly TextBox _mobile = new();
    private readonly TextBox _location = MultilineBox(70);
    private readonly CheckBox _superuserMode = new() { Content = "Superuser Mode", Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBox _poycText = MultilineBox(70);
    private readonly TextBox _ddaText = MultilineBox(70);
    private readonly TextBox _customTextFilePath = new();
    private readonly TextBox _orderCsvPath = new();
    private readonly List<CheckBox> _prepChecks = [];

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();

        Title = "Futuristic HUD Settings";
        Width = 720;
        Height = 720;
        MinWidth = 620;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Content = BuildContent();
        LoadValues();
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
        var save = new Button { Content = "Save", Width = 96, Height = 34, Margin = new Thickness(8, 0, 0, 0) };
        var cancel = new Button { Content = "Cancel", Width = 96, Height = 34, Margin = new Thickness(8, 0, 0, 0) };
        save.Click += (_, _) => SaveAndClose();
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var content = new StackPanel();
        scroll.Content = content;
        root.Children.Add(scroll);

        content.Children.Add(Header("Menu slices"));
        content.Children.Add(Text("Choose a preset/action for each slice. Selecting a preset automatically renames that slice. The Settings slice is kept available so you can always get back here."));

        for (var i = 0; i < _settings.MenuItems.Count; i++)
        {
            content.Children.Add(BuildMenuRow(i));
        }

        content.Children.Add(Header("General Settings"));
        content.Children.Add(_superuserMode);
        content.Children.Add(Text("Superuser Mode unlocks receipt preview thumbnails and thermal test print tools."));
        content.Children.Add(BuildLogoRow());
        content.Children.Add(Field("QR website", _qrWebsite));
        content.Children.Add(Field("QR-only caption", _qrCaption));
        content.Children.Add(Field("Email", _email));
        content.Children.Add(Field("Mobile number", _mobile));
        content.Children.Add(Field("Address", _location));

        content.Children.Add(Header("Preset Details"));
        content.Children.Add(Field("POYC text", _poycText));
        content.Children.Add(Field("DDA text", _ddaText));
        content.Children.Add(BuildFileRow("Custom TxT/PDF", _customTextFilePath, "Text/PDF files|*.txt;*.pdf|Text files|*.txt|PDF files|*.pdf|All files|*.*"));
        content.Children.Add(BuildFileRow("Order CSV", _orderCsvPath, "CSV files|*.csv;*.txt|All files|*.*"));

        content.Children.Add(Header("Preparation Checklist"));
        content.Children.Add(Text("Use this as a setup list when installing on a new PC or checking what still needs changing."));
        content.Children.Add(BuildPrepChecklist());

        content.Children.Add(Header("Install Defaults"));
        content.Children.Add(Text("Install defaults are stored beside the EXE as default-settings.json. New PCs use those defaults automatically, and reset restores them."));
        var defaultButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var saveDefaults = new Button { Content = "Save Current as Install Defaults", Width = 210, Height = 34, Margin = new Thickness(0, 0, 8, 0) };
        var resetDefaults = new Button { Content = "Reset to Install Defaults", Width = 175, Height = 34 };
        saveDefaults.Click += (_, _) => SaveCurrentAsInstallDefaults();
        resetDefaults.Click += (_, _) => ResetToInstallDefaults();
        defaultButtons.Children.Add(saveDefaults);
        defaultButtons.Children.Add(resetDefaults);
        content.Children.Add(defaultButtons);

        return root;
    }

    private UIElement BuildMenuRow(int index)
    {
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

        var label = new TextBox { Margin = new Thickness(0, 0, 8, 0) };
        var action = new ComboBox
        {
            DisplayMemberPath = nameof(ActionDefinition.Name),
            SelectedValuePath = nameof(ActionDefinition.Key),
            ItemsSource = HudConfig.AvailableActions,
            Margin = new Thickness(0, 0, 8, 0)
        };
        action.SelectionChanged += (_, _) =>
        {
            if (_loading || action.SelectedItem is not ActionDefinition selected)
            {
                return;
            }

            label.Text = selected.Name;
        };
        var accent = new ColorPickerButton();
        _labelBoxes.Add(label);
        _actionBoxes.Add(action);
        _accentButtons.Add(accent);

        Grid.SetColumn(label, 0);
        Grid.SetColumn(action, 1);
        Grid.SetColumn(accent, 2);
        grid.Children.Add(label);
        grid.Children.Add(action);
        grid.Children.Add(accent);
        return grid;
    }

    private UIElement BuildLogoRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.Children.Add(new TextBlock { Text = "Logo image", VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(_logoPath, 1);
        grid.Children.Add(_logoPath);
        var browse = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
        browse.Click += (_, _) =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
                Title = "Choose logo image"
            };
            if (dialog.ShowDialog() == true)
            {
                _logoPath.Text = dialog.FileName;
            }
        };
        Grid.SetColumn(browse, 2);
        grid.Children.Add(browse);
        return grid;
    }

    private static UIElement BuildFileRow(string label, TextBox box, string filter)
    {
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(box, 1);
        grid.Children.Add(box);
        var browse = new Button { Content = "Browse", Margin = new Thickness(8, 0, 0, 0) };
        browse.Click += (_, _) =>
        {
            var dialog = new OpenFileDialog { Filter = filter, Title = $"Choose {label}" };
            if (dialog.ShowDialog() == true)
            {
                box.Text = dialog.FileName;
            }
        };
        Grid.SetColumn(browse, 2);
        grid.Children.Add(browse);
        return grid;
    }

    private static UIElement Field(string label, TextBox box)
    {
        var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(box, 1);
        grid.Children.Add(box);
        return grid;
    }

    private UIElement BuildPrepChecklist()
    {
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddPrepCell(grid, "Done", 0, 0, bold: true);
        AddPrepCell(grid, "Setting", 0, 1, bold: true);
        AddPrepCell(grid, "What is needed", 0, 2, bold: true);

        for (var i = 0; i < _settings.PrepChecklist.Count; i++)
        {
            var row = i + 1;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var check = new CheckBox { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 0, 6) };
            _prepChecks.Add(check);
            Grid.SetRow(check, row);
            Grid.SetColumn(check, 0);
            grid.Children.Add(check);
            AddPrepCell(grid, _settings.PrepChecklist[i].Name, row, 1, bold: false);
            AddPrepCell(grid, _settings.PrepChecklist[i].Needed, row, 2, bold: false);
        }

        return grid;
    }

    private static void AddPrepCell(Grid grid, string text, int row, int column, bool bold)
    {
        if (grid.RowDefinitions.Count <= row)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var block = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Margin = new Thickness(4, 5, 4, 5)
        };
        Grid.SetRow(block, row);
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }

    private static TextBox MultilineBox(double height) => new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        MinHeight = height
    };

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        FontSize = 19,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 18, 0, 8)
    };

    private static TextBlock Text(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.8,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private void LoadValues()
    {
        _loading = true;
        for (var i = 0; i < _settings.MenuItems.Count; i++)
        {
            _labelBoxes[i].Text = _settings.MenuItems[i].Label;
            _actionBoxes[i].SelectedValue = _settings.MenuItems[i].ActionKey;
            _accentButtons[i].SelectedColorHex = _settings.MenuItems[i].AccentHex;
        }
        _loading = false;

        _superuserMode.IsChecked = _settings.Presets.SuperuserMode;
        _logoPath.Text = _settings.Presets.LogoPath;
        _qrWebsite.Text = _settings.Presets.QrWebsite;
        _qrCaption.Text = _settings.Presets.QrCaption;
        _email.Text = _settings.Presets.Email;
        _mobile.Text = _settings.Presets.Mobile;
        _location.Text = _settings.Presets.Location;
        _poycText.Text = _settings.Presets.PoycText;
        _ddaText.Text = _settings.Presets.DdaText;
        _customTextFilePath.Text = _settings.Presets.CustomTextFilePath;
        _orderCsvPath.Text = _settings.Presets.OrderCsvPath;
        for (var i = 0; i < _prepChecks.Count && i < _settings.PrepChecklist.Count; i++)
        {
            _prepChecks[i].IsChecked = _settings.PrepChecklist[i].Done;
        }
    }

    private void SaveAndClose()
    {
        SaveSettings(showMessage: true);
        Close();
    }

    public void SaveState()
    {
        SaveSettings(showMessage: false);
    }

    private void SaveSettings(bool showMessage)
    {
        var menu = new List<MenuItemSetting>();
        for (var i = 0; i < _labelBoxes.Count; i++)
        {
            var actionKey = _actionBoxes[i].SelectedValue?.ToString() ?? "print_option_1";
            menu.Add(new MenuItemSetting(
                string.IsNullOrWhiteSpace(_labelBoxes[i].Text) ? $"Option {i + 1}" : _labelBoxes[i].Text.Trim(),
                actionKey,
                _accentButtons[i].SelectedColorHex));
        }

        if (!menu.Any(item => item.ActionKey == "open_settings"))
        {
            menu.Add(new MenuItemSetting("Settings", "open_settings", "#CFF6FF"));
        }

        _settings.MenuItems = menu;
        _settings.Presets.SuperuserMode = _superuserMode.IsChecked == true;
        _settings.Presets.LogoPath = _logoPath.Text.Trim();
        _settings.Presets.QrWebsite = _qrWebsite.Text.Trim();
        _settings.Presets.QrCaption = _qrCaption.Text.Trim();
        _settings.Presets.Email = _email.Text.Trim();
        _settings.Presets.Mobile = _mobile.Text.Trim();
        _settings.Presets.Location = _location.Text.Trim();
        _settings.Presets.PoycText = _poycText.Text.Trim();
        _settings.Presets.DdaText = _ddaText.Text.Trim();
        _settings.Presets.CustomTextFilePath = _customTextFilePath.Text.Trim();
        _settings.Presets.OrderCsvPath = _orderCsvPath.Text.Trim();
        for (var i = 0; i < _prepChecks.Count && i < _settings.PrepChecklist.Count; i++)
        {
            _settings.PrepChecklist[i].Done = _prepChecks[i].IsChecked == true;
        }
        _settings.Save();

        if (showMessage)
        {
            MessageBox.Show("Settings saved. Reopen the HUD to see menu changes.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SaveCurrentAsInstallDefaults()
    {
        SaveSettings(showMessage: false);
        try
        {
            _settings.SaveAsInstallDefaults();
            MessageBox.Show("Current settings saved as install defaults. Copy this app folder to a new PC to keep these defaults.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save install defaults:\n{ex.Message}", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetToInstallDefaults()
    {
        var defaults = AppSettings.LoadInstallDefaults();
        defaults.Save();
        MessageBox.Show("Settings reset to install defaults. Reopen Settings to view them.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}
