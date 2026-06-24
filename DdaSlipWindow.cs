using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class DdaSlipWindow : Window, IAutoSaveWindow
{
    private readonly AppSettings _settings;
    private readonly TextBox _id = new();
    private readonly TextBox _eur = new();
    private readonly TextBlock _feeError = new() { FontSize = 11, Visibility = Visibility.Collapsed };
    private readonly CheckBox _superuser = new() { Content = "SuperUser \ud83e\uddb8\u200d\u2640\ufe0f", Margin = new Thickness(0, 0, 0, 8) };
    private readonly CheckBox _printDetails = new() { Content = "Print DDA Info \u2139", Margin = new Thickness(0, 2, 0, 8) };
    private readonly CheckBox _renewCard = new() { Content = "Renew DDA Card (Fee)", Margin = new Thickness(0, 2, 0, 8) };
    private readonly StackPanel _linesHost = new();
    private readonly Border _suggestionHost = new() { Visibility = Visibility.Collapsed };
    private readonly WrapPanel _suggestionGroups = new();
    private readonly TextBlock _medicineStatus = new() { Margin = new Thickness(2, 4, 0, 0), FontSize = 12, Visibility = Visibility.Collapsed };
    private readonly List<DdaLineEditor> _lineEditors = [];
    private TextBox? _activeMedicineBox;
    private bool _eurFirstEdit = true;
    private bool _confirmFeesRequested;
    private bool _updatingMedicineText;

    public DdaSlipWindow(AppSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();

        Title = "DDA Slip";
        Width = 700;
        Height = 720;
        MinWidth = 520;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = (Brush)new BrushConverter().ConvertFromString("#F9FAFB")!;
        Content = BuildContent();
        AddLine();
        _id.Focus();
    }

    public void SaveState()
    {
        SaveMedicineNamesFromRows();
    }

    public static void RunPrintVisualSelfTest()
    {
        var settings = AppSettings.CreateDefault();
        settings.Presets.SuperuserMode = true;
        settings.Presets.DdaText = "DDA information self-test";
        var window = new DdaSlipWindow(settings);
        var visual = window.BuildPrintout(
            "AB123",
            [
                new DdaSlipLine("AMOXICILLIN 500MG", 2),
                new DdaSlipLine("CO-CODAMOL 8 500", 1.5m)
            ],
            0,
            confirmFees: true,
            printDetails: true,
            renewCard: true,
            width: 420);
        visual.Measure(new Size(420, double.PositiveInfinity));
        visual.Arrange(new Rect(new Point(0, 0), visual.DesiredSize));
        visual.UpdateLayout();
        window.Close();
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(22) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        var cancel = Button("Cancel", "#FFFFFF", 110);
        var print = Button("Print", "#DBEAFE", 124);
        cancel.Click += (_, _) => Close();
        print.Click += (_, _) => PrintAndClose();
        buttons.Children.Add(cancel);
        buttons.Children.Add(print);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var content = new StackPanel
        {
            Background = Brushes.White,
            Margin = new Thickness(0),
        };
        root.Children.Add(scroll);

        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
            BorderThickness = new Thickness(1.4),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(20),
            Child = content
        };
        scroll.Content = card;

        content.Children.Add(new TextBlock
        {
            Text = "DDA",
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#111827")!,
            Margin = new Thickness(0, 0, 0, 6)
        });
        _superuser.IsChecked = _settings.Presets.SuperuserMode;
        _superuser.Checked += (_, _) => UpdateSuperuserControls();
        _superuser.Unchecked += (_, _) => UpdateSuperuserControls();
        content.Children.Add(_superuser);
        content.Children.Add(Text("Fill this slip quickly, print, and the temporary ID, quantity, and EUR values are discarded. Medicine names are kept in capitals for future search."));

        _id.TextChanged += (_, _) => SanitizeBox(_id, allowSpaces: false, forceUpper: true);
        _eur.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Back or Key.Delete or Key.D0 or Key.D1 or Key.D2 or Key.D3 or Key.D4 or Key.D5 or Key.D6 or Key.D7 or Key.D8 or Key.D9 or Key.NumPad0 or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4 or Key.NumPad5 or Key.NumPad6 or Key.NumPad7 or Key.NumPad8 or Key.NumPad9)
            {
                _confirmFeesRequested = false;
            }
        };
        _eur.PreviewTextInput += (_, e) =>
        {
            if (_eurFirstEdit && _eur.Text == "0")
            {
                _eur.Clear();
            }
            _eurFirstEdit = false;
            _confirmFeesRequested = false;
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                SetFeeError("Numbers only.");
            }
        };
        DataObject.AddPastingHandler(_eur, (_, e) =>
        {
            if (_eurFirstEdit && _eur.Text == "0")
            {
                _eur.Clear();
            }
            _eurFirstEdit = false;
            _confirmFeesRequested = false;
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                SetFeeError("Numbers only.");
                return;
            }

            var text = e.DataObject.GetData(DataFormats.Text)?.ToString()?.Trim() ?? "";
            if (text.Length == 0 || !text.All(char.IsDigit))
            {
                e.CancelCommand();
                SetFeeError("Numbers only.");
            }
        });
        _eur.TextChanged += (_, _) => SanitizeEuroBox(_eur);
        _eur.Text = "0";
        content.Children.Add(BuildIdFeesRow());
        content.Children.Add(BuildOptionRow());

        var header = new Grid { Margin = new Thickness(0, 14, 0, 4) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
        AddHeader(header, "\ud83d\udc8a Medicines", 0, "Medicines");
        AddHeader(header, "QTY", 1, "Quantity. Numbers only, rounded to two decimal places.");
        AddHeader(header, "Actions", 2, "Add or remove a medicine row.");
        content.Children.Add(header);
        content.Children.Add(_linesHost);

        _suggestionHost.Child = new ScrollViewer
        {
            MaxHeight = 176,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _suggestionGroups
        };
        _suggestionHost.Background = (Brush)new BrushConverter().ConvertFromString("#F8FAFC")!;
        _suggestionHost.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#D1D5DB")!;
        _suggestionHost.BorderThickness = new Thickness(1);
        _suggestionHost.CornerRadius = new CornerRadius(10);
        _suggestionHost.Padding = new Thickness(10);
        _suggestionHost.Margin = new Thickness(0, 8, 0, 0);
        content.Children.Add(_suggestionHost);
        content.Children.Add(_medicineStatus);
        UpdateSuperuserControls();
        content.Children.Add(Text("DDA Details text is edited in Settings and is only included when this box is ticked."));
        return root;
    }

    private void AddLine()
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });

        var medicine = new TextBox
        {
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Medicines",
            FontSize = 16,
            CharacterCasing = CharacterCasing.Upper
        };
        var quantity = new TextBox
        {
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = "Quantity. Numbers only, rounded to two decimal places.",
            FontSize = 16
        };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0) };
        var add = MiniButton("+", "#ECFDF5");
        var remove = MiniButton("-", "#FFF4E6");
        var editor = new DdaLineEditor(row, medicine, quantity);

        medicine.GotFocus += (_, _) =>
        {
            _activeMedicineBox = medicine;
            UpdateSuggestions(medicine.Text);
        };
        medicine.TextChanged += (_, _) =>
        {
            SanitizeMedicineBox(medicine);
            _activeMedicineBox = medicine;
            UpdateSuggestions(medicine.Text);
        };
        medicine.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SaveMedicineName(medicine.Text);
                quantity.Focus();
                e.Handled = true;
            }
        };
        quantity.TextChanged += (_, _) => SanitizeQuantityBox(quantity);
        add.Click += (_, _) => AddLine();
        remove.Click += (_, _) =>
        {
            if (_lineEditors.Count <= 1)
            {
                medicine.Text = "";
                quantity.Text = "";
                return;
            }

            _linesHost.Children.Remove(row);
            _lineEditors.Remove(editor);
        };

        Grid.SetColumn(medicine, 0);
        Grid.SetColumn(quantity, 1);
        actions.Children.Add(add);
        actions.Children.Add(remove);
        Grid.SetColumn(actions, 2);
        row.Children.Add(medicine);
        row.Children.Add(quantity);
        row.Children.Add(actions);
        _lineEditors.Add(editor);
        _linesHost.Children.Add(row);
        medicine.Focus();
    }

    private void UpdateSuggestions(string query)
    {
        query = AppSettings.SanitizeAlphanumeric(query, allowSpaces: true).ToUpperInvariant();
        if (_settings.Presets.DdaMedicineNames.Count == 0)
        {
            _suggestionHost.Visibility = Visibility.Collapsed;
            _medicineStatus.Visibility = Visibility.Collapsed;
            return;
        }

        if (query.Length == 0 && !IsSuperuser)
        {
            _suggestionHost.Visibility = Visibility.Collapsed;
            _medicineStatus.Visibility = Visibility.Collapsed;
            return;
        }

        var matches = (query.Length == 0
                ? _settings.Presets.DdaMedicineNames
                : _settings.Presets.DdaMedicineNames.Where(name => name.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Take(IsSuperuser ? 12 : 8)
            .ToList();
        PopulateSuggestionItems(matches);
        _suggestionHost.Visibility = matches.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        _medicineStatus.Visibility = Visibility.Visible;
        if (matches.Count == 0)
        {
            _medicineStatus.Text = "0 results. This will print as entered and save as a new medicine name.";
            _medicineStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#EF4444")!;
        }
        else
        {
            _medicineStatus.Text = query.Length == 0 && IsSuperuser
                ? "SuperUser: select a saved medicine name to remove it."
                : $"{matches.Count} saved suggestion{(matches.Count == 1 ? "" : "s")} found.";
            _medicineStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#14B8A6")!;
        }
        if (matches.Count > 0)
        {
        }
    }

    private bool IsSuperuser => _superuser.IsChecked == true || _settings.Presets.SuperuserMode;

    private void UpdateSuperuserControls()
    {
        UpdateSuggestions(_activeMedicineBox?.Text ?? "");
    }

    private void PopulateSuggestionItems(IReadOnlyList<string> matches)
    {
        _suggestionGroups.Children.Clear();
        foreach (var group in matches.GroupBy(GroupKey).OrderBy(group => group.Key))
        {
            _suggestionGroups.Children.Add(BuildSuggestionGroup(group.Key, group.OrderBy(name => name).ToList()));
        }
    }

    private UIElement BuildSuggestionGroup(string key, IReadOnlyList<string> names)
    {
        var panel = new StackPanel
        {
            Width = 190,
            Margin = new Thickness(0, 0, 18, 10)
        };
        panel.Children.Add(new TextBlock
        {
            Text = key,
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#111827")!,
            Margin = new Thickness(0, 0, 0, 6)
        });
        var chips = new WrapPanel();
        foreach (var name in names)
        {
            chips.Children.Add(BuildSuggestionChip(name));
        }
        panel.Children.Add(chips);
        return panel;
    }

    private UIElement BuildSuggestionChip(string name)
    {
        var chip = new Border
        {
            Background = Brushes.White,
            BorderBrush = (Brush)new BrushConverter().ConvertFromString("#CBD5E1")!,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(8, 5, 8, 5)
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var pick = ChipButton(name, "#374151");
        pick.Click += (_, _) => SelectMedicineSuggestion(name);
        row.Children.Add(pick);

        if (IsSuperuser)
        {
            var remove = ChipButton("Remove", "#DC2626");
            remove.Margin = new Thickness(8, 0, 0, 0);
            remove.Click += (_, _) => RemoveSavedMedicine(name);
            row.Children.Add(remove);
        }

        chip.Child = row;
        return chip;
    }

    private void SelectMedicineSuggestion(string name)
    {
        if (_activeMedicineBox is null)
        {
            return;
        }

        _activeMedicineBox.Text = name;
        _activeMedicineBox.CaretIndex = _activeMedicineBox.Text.Length;
        _suggestionHost.Visibility = Visibility.Collapsed;
    }

    private static string GroupKey(string name)
    {
        var first = name.FirstOrDefault();
        return char.IsLetter(first) ? char.ToUpperInvariant(first).ToString() : "#";
    }

    private void RemoveSavedMedicine(string selected)
    {
        if (!IsSuperuser)
        {
            return;
        }

        var clean = NormalizeSavedMedicine(selected);
        var before = _settings.Presets.DdaMedicineNames.Count;
        _settings.Presets.DdaMedicineNames = _settings.Presets.DdaMedicineNames
            .Where(name => !name.Equals(clean, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_settings.Presets.DdaMedicineNames.Count == before)
        {
            return;
        }

        _settings.Save();
        _medicineStatus.Text = $"Removed saved medicine: {clean}";
        _medicineStatus.Foreground = (Brush)new BrushConverter().ConvertFromString("#DC2626")!;
        _medicineStatus.Visibility = Visibility.Visible;
        UpdateSuggestions(_activeMedicineBox?.Text ?? "");
    }

    private void PrintAndClose()
    {
        var lines = CurrentLines();
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var panel = BuildPrintout(
                AppSettings.SanitizeAlphanumeric(_id.Text, allowSpaces: false).ToUpperInvariant(),
                lines,
                ParseEuro(_eur.Text),
                _confirmFeesRequested,
                _printDetails.IsChecked == true,
                _renewCard.IsChecked == true,
                Math.Min(dialog.PrintableAreaWidth, 420));
            ReceiptPreviewWindow.PrintVisual(dialog, panel, "DDA");
            SaveMedicineNames(lines.Select(line => line.Medicine));
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not print DDA slip:\n{ex.Message}", "DDA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<DdaSlipLine> CurrentLines() =>
        _lineEditors
            .Select(editor => new DdaSlipLine(
                AppSettings.SanitizeAlphanumeric(editor.Medicine.Text, allowSpaces: true).ToUpperInvariant(),
                ParseQuantity(editor.Quantity.Text)))
            .Where(line => !string.IsNullOrWhiteSpace(line.Medicine) || line.Quantity > 0)
            .ToList();

    private void SaveMedicineNamesFromRows()
    {
        SaveMedicineNames(_lineEditors.Select(editor => editor.Medicine.Text));
    }

    private void SaveMedicineName(string medicine)
    {
        SaveMedicineNames([medicine]);
    }

    private void SaveMedicineNames(IEnumerable<string> medicines)
    {
        var changed = false;
        foreach (var medicine in medicines)
        {
            var clean = NormalizeSavedMedicine(medicine);
            if (string.IsNullOrWhiteSpace(clean) ||
                _settings.Presets.DdaMedicineNames.Contains(clean, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            _settings.Presets.DdaMedicineNames.Add(clean);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        _settings.Presets.DdaMedicineNames = _settings.Presets.DdaMedicineNames
            .Select(NormalizeSavedMedicine)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
        _settings.Save();
    }

    private FrameworkElement BuildPrintout(string id, IReadOnlyList<DdaSlipLine> lines, int euro, bool confirmFees, bool printDetails, bool renewCard, double width)
    {
        var panel = new StackPanel
        {
            Width = width,
            Background = Brushes.White,
            Margin = new Thickness(0)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "DDA",
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(8, 8, 8, 4)
        });

        panel.Children.Add(PrintLine($"\ud83c\udd94 ID: {BlankDash(id)}", 13, FontWeights.SemiBold));
        panel.Children.Add(PrintLine($"EUR \ud83d\udcb6: {FormatEuro(euro, confirmFees)}", 13, FontWeights.SemiBold));
        if (renewCard)
        {
            panel.Children.Add(PrintLine("Renew DDA Card", 13, FontWeights.SemiBold));
        }
        panel.Children.Add(new Border { Height = 1, Background = Brushes.Black, Margin = new Thickness(8, 6, 8, 6) });

        var grid = new Grid { Margin = new Thickness(8, 0, 8, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        AddPrintCell(grid, "\ud83d\udc8a Medicines", 0, 0, true);
        AddPrintCell(grid, "QTY", 0, 1, true);

        if (lines.Count == 0)
        {
            AddPrintCell(grid, "No medicines entered", 1, 0, false);
            AddPrintCell(grid, "", 1, 1, false);
        }
        else
        {
            for (var i = 0; i < lines.Count; i++)
            {
                AddPrintCell(grid, BlankDash(lines[i].Medicine), i + 1, 0, false);
                AddPrintCell(grid, FormatQuantity(lines[i].Quantity), i + 1, 1, false, TextAlignment.Right);
            }
        }

        panel.Children.Add(grid);

        if (printDetails)
        {
            panel.Children.Add(new Border { Height = 1, Background = Brushes.Black, Margin = new Thickness(8, 8, 8, 6) });
            panel.Children.Add(PrintLine("Printing DDA \u2139", 12, FontWeights.SemiBold));
            panel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_settings.Presets.DdaText) ? "No DDA details configured." : _settings.Presets.DdaText.Trim(),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                TextAlignment = TextAlignment.Justify,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 2, 8, 8)
            });
        }

        panel.Children.Add(new Border { Height = 10 });
        return panel;
    }

    private static void AddPrintCell(Grid grid, string text, int row, int column, bool bold, TextAlignment alignment = TextAlignment.Left)
    {
        while (grid.RowDefinitions.Count <= row)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var border = new Border
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0, row == 0 ? 0 : 1, 0, 0),
            Padding = new Thickness(4, 5, 4, 5),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = row == 0 ? 12 : 13,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                TextAlignment = alignment,
                TextWrapping = TextWrapping.Wrap
            }
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    private UIElement BuildIdFeesRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 10) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });

        var idField = Field("\ud83c\udd94 ID", _id, "Capital letters and numbers only.");
        var feeField = FeesField();
        Grid.SetColumn(idField, 0);
        Grid.SetColumn(feeField, 2);
        grid.Children.Add(idField);
        grid.Children.Add(feeField);
        return grid;
    }

    private UIElement BuildOptionRow()
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _renewCard.Content = "Renew DDA Card (Fee)";
        _printDetails.Content = "Print DDA Info \u2139";
        Grid.SetColumn(_renewCard, 0);
        Grid.SetColumn(_printDetails, 1);
        grid.Children.Add(_renewCard);
        grid.Children.Add(_printDetails);
        return grid;
    }

    private UIElement FeesField()
    {
        _eur.ToolTip = "Fees: whole numbers from 0 to 999.";
        _eur.FontSize = 16;
        var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
        panel.Children.Add(new TextBlock
        {
            Text = "EUR \ud83d\udcb6",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#6B7280")!,
            Margin = new Thickness(2, 0, 0, 6)
        });
        panel.Children.Add(_eur);

        var helper = new Grid { Margin = new Thickness(2, 4, 0, 0) };
        helper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        helper.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _feeError.Foreground = (Brush)new BrushConverter().ConvertFromString("#EF4444")!;
        helper.Children.Add(_feeError);

        var confirm = TextActionButton("Confirm Fees", "Confirm fees");
        confirm.Click += (_, _) =>
        {
            _confirmFeesRequested = true;
            SetFeeError("Confirm Fees selected.", "#DC2626");
        };
        Grid.SetColumn(confirm, 1);
        helper.Children.Add(confirm);
        panel.Children.Add(helper);
        return panel;
    }

    private static UIElement Field(string label, TextBox box, string tooltip)
    {
        box.ToolTip = tooltip;
        box.FontSize = 16;
        var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 10) };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#6B7280")!,
            Margin = new Thickness(2, 0, 0, 6)
        });
        panel.Children.Add(box);
        panel.Children.Add(new TextBlock
        {
            Text = tooltip,
            FontSize = 11,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#9CA3AF")!,
            Margin = new Thickness(2, 4, 0, 0)
        });
        return panel;
    }

    private static void AddHeader(Grid grid, string text, int column, string tooltip)
    {
        var block = new TextBlock
        {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        FontSize = 18,
        Foreground = (Brush)new BrushConverter().ConvertFromString("#6B7280")!,
        Margin = new Thickness(0, 0, 8, 0),
        ToolTip = tooltip
        };
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }

    private static Button Button(string label, string color, double width) => new()
    {
        Content = label,
        Width = width,
        Height = 44,
        Margin = new Thickness(8, 0, 0, 0),
        Background = (Brush)new BrushConverter().ConvertFromString(color)!,
        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#D1D5DB")!,
        FontWeight = FontWeights.SemiBold
    };

    private static Button MiniButton(string label, string color) => new()
    {
        Content = label,
        Width = 42,
        Height = 44,
        Margin = new Thickness(4, 0, 0, 0),
        FontSize = 20,
        FontWeight = FontWeights.Bold,
        Background = (Brush)new BrushConverter().ConvertFromString(color)!,
        BorderBrush = (Brush)new BrushConverter().ConvertFromString("#D1D5DB")!,
        Foreground = (Brush)new BrushConverter().ConvertFromString("#111827")!
    };

    private static Button ChipButton(string label, string color)
    {
        var text = new TextBlock
        {
            Text = label,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString(color)!,
            TextWrapping = TextWrapping.NoWrap
        };
        return new Button
        {
            Content = text,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Padding = new Thickness(0),
            MinHeight = 24,
            ToolTip = label
        };
    }

    private static Button TextActionButton(string label, string automationName)
    {
        var text = new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)new BrushConverter().ConvertFromString("#DC2626")!
        };
        var button = new Button
        {
            Content = text,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Padding = new Thickness(4, 0, 0, 0),
            MinHeight = 26,
            ToolTip = automationName
        };
        button.MouseEnter += (_, _) => text.TextDecorations = TextDecorations.Underline;
        button.MouseLeave += (_, _) =>
        {
            if (!button.IsKeyboardFocused)
            {
                text.TextDecorations = null;
            }
        };
        button.GotKeyboardFocus += (_, _) => text.TextDecorations = TextDecorations.Underline;
        button.LostKeyboardFocus += (_, _) => text.TextDecorations = null;
        return button;
    }

    private void SetFeeError(string message, string color = "#EF4444")
    {
        _feeError.Text = message;
        _feeError.Foreground = (Brush)new BrushConverter().ConvertFromString(color)!;
        _feeError.Visibility = Visibility.Visible;
    }

    private void ClearFeeError()
    {
        _feeError.Text = "";
        _feeError.Visibility = Visibility.Collapsed;
    }

    private static TextBlock Text(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.76,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private static TextBlock PrintLine(string text, double fontSize, FontWeight weight) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Segoe UI"),
        FontSize = fontSize,
        FontWeight = weight,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(8, 2, 8, 2)
    };

    private static void SanitizeBox(TextBox box, bool allowSpaces, bool forceUpper)
    {
        var clean = AppSettings.SanitizeAlphanumeric(box.Text, allowSpaces);
        if (forceUpper)
        {
            clean = clean.ToUpperInvariant();
        }

        SetTextPreservingCaret(box, clean);
    }

    private void SanitizeMedicineBox(TextBox box)
    {
        if (_updatingMedicineText)
        {
            return;
        }

        var clean = SanitizeMedicineForEditing(box.Text);
        if (clean == box.Text)
        {
            return;
        }

        _updatingMedicineText = true;
        SetTextPreservingCaret(box, clean);
        _updatingMedicineText = false;
    }

    private static string SanitizeMedicineForEditing(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var chars = new List<char>(value.Length);
        var previousWasSpace = false;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars.Add(char.ToUpperInvariant(ch));
                previousWasSpace = false;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !previousWasSpace)
            {
                chars.Add(' ');
                previousWasSpace = true;
            }
        }

        return new string(chars.ToArray());
    }

    private static string NormalizeSavedMedicine(string value) =>
        AppSettings.SanitizeAlphanumeric(value, allowSpaces: true).ToUpperInvariant();

    private static void SanitizeQuantityBox(TextBox box)
    {
        var text = box.Text.Replace(',', '.');
        var output = new List<char>(text.Length);
        var hasDecimal = false;
        var decimals = 0;
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
            {
                if (hasDecimal)
                {
                    if (decimals >= 2)
                    {
                        continue;
                    }

                    decimals++;
                }
                output.Add(ch);
                continue;
            }

            if (ch == '.' && !hasDecimal)
            {
                hasDecimal = true;
                output.Add('.');
            }
        }

        SetTextPreservingCaret(box, new string(output.ToArray()));
    }

    private void SanitizeEuroBox(TextBox box)
    {
        var text = box.Text.Trim();
        var output = new List<char>(text.Length);
        foreach (var ch in text)
        {
            if (char.IsDigit(ch))
            {
                output.Add(ch);
            }
        }

        var clean = new string(output.ToArray());
        if (clean != text && text.Length > 0)
        {
            SetFeeError("Numbers only.");
        }

        if (clean.Length == 0)
        {
            SetTextPreservingCaret(box, clean);
            if (!_confirmFeesRequested)
            {
                ClearFeeError();
            }
            return;
        }

        if (int.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 999)
        {
            clean = "999";
            SetFeeError("Maximum fee is 999.");
        }
        else if (!_confirmFeesRequested && clean == text)
        {
            ClearFeeError();
        }

        SetTextPreservingCaret(box, clean);
    }

    private static void SetTextPreservingCaret(TextBox box, string clean)
    {
        if (box.Text == clean)
        {
            return;
        }

        var caret = Math.Min(clean.Length, box.CaretIndex);
        box.Text = clean;
        box.CaretIndex = caret;
    }

    private static decimal ParseQuantity(string text)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity))
        {
            return 0;
        }

        return Math.Round(Math.Max(0, quantity), 2);
    }

    private static int ParseEuro(string text)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var euro))
        {
            return 0;
        }

        return Math.Clamp(euro, 0, 999);
    }

    private static string FormatEuro(int euro, bool confirmFees)
    {
        if (confirmFees)
        {
            return "Confirm Fees";
        }

        return euro == 0 ? "- - -" : euro.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatQuantity(decimal quantity) =>
        quantity <= 0 ? "- - -" : quantity.ToString("0.##", CultureInfo.InvariantCulture);

    private static string BlankDash(string value) => string.IsNullOrWhiteSpace(value) ? "- - -" : value.Trim();

    private sealed record DdaLineEditor(Grid Row, TextBox Medicine, TextBox Quantity);

    private sealed record DdaSlipLine(string Medicine, decimal Quantity);
}
