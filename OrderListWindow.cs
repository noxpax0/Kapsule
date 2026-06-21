using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FuturisticCtrlHud;

public sealed class OrderListWindow : Window, IAutoSaveWindow
{
    private readonly AppSettings _settings;
    private readonly ObservableCollection<OrderLineItem> _items;
    private readonly List<ProductReference> _products;
    private readonly TextBox _search = new();
    private readonly ListBox _matches = new() { Height = 160 };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 260 };
    private readonly ProgressBar _profitBar = new() { Minimum = 0, Maximum = 100, Height = 24 };
    private readonly TextBlock _profitText = new() { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };

    public OrderListWindow(AppSettings settings)
    {
        _settings = settings;
        _settings.EnsureDefaults();
        _items = new ObservableCollection<OrderLineItem>(_settings.Presets.OrderItems);
        _products = LoadProducts(_settings.Presets.OrderCsvPath);

        Title = "Order List";
        Width = 820;
        Height = 720;
        MinWidth = 700;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
        RefreshMatches();
    }

    private UIElement BuildContent()
    {
        var root = new DockPanel { Margin = new Thickness(18) };
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        bottom.Children.Add(Button("Delete Line", "#FFEAEA", (_, _) => DeleteSelected()));
        bottom.Children.Add(Button("- Qty", "#FFF4DE", (_, _) => ChangeQty(-1)));
        bottom.Children.Add(Button("+ Qty", "#ECFDF5", (_, _) => ChangeQty(1)));
        bottom.Children.Add(Button("Clear List", "#FFEAEA", (_, _) => ClearList()));
        bottom.Children.Add(Button("Print", "#E8F1FF", (_, _) => PrintList()));
        bottom.Children.Add(Button("Close", "#F6F8FA", (_, _) => Close()));
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = "Order List", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) });
        content.Children.Add(new TextBlock { Text = $"CSV: {(_settings.Presets.OrderCsvPath.Length == 0 ? "No CSV selected in Settings" : _settings.Presets.OrderCsvPath)}", TextWrapping = TextWrapping.Wrap, Opacity = 0.75, Margin = new Thickness(0, 0, 0, 10) });

        var searchRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(175) });
        _search.Height = 48;
        _search.FontSize = 24;
        _search.FontWeight = FontWeights.SemiBold;
        _search.Padding = new Thickness(10, 4, 10, 4);
        searchRow.Children.Add(_search);
        var clientOrder = Button("\U0001F6D2 Client Order", "#FFF4DE", (_, _) => ToggleClientOrder(), width: 160);
        clientOrder.Height = 48;
        clientOrder.FontWeight = FontWeights.SemiBold;
        Grid.SetColumn(clientOrder, 1);
        searchRow.Children.Add(clientOrder);
        content.Children.Add(searchRow);

        _search.Margin = new Thickness(0, 0, 0, 6);
        _search.ToolTip = "Type product name. If no match is found, press Enter or Add Custom to keep it in uppercase without updating the CSV.";
        _search.TextChanged += (_, _) => RefreshMatches();
        _search.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddBestOrCustom();
            }
        };
        _matches.SelectionChanged += (_, _) => UpdateProfitVisual();
        content.Children.Add(_matches);
        _matches.MouseDoubleClick += (_, _) => AddSelectedMatch();
        content.Children.Add(Button("Add Best Match / Custom", "#ECFDF5", (_, _) => AddBestOrCustom(), width: 190));

        content.Children.Add(_profitText);
        content.Children.Add(_profitBar);

        ConfigureGrid();
        _grid.ItemsSource = _items;
        _grid.SelectionChanged += (_, _) => UpdateProfitVisual();
        content.Children.Add(_grid);
        root.Children.Add(content);
        return root;
    }

    private void ConfigureGrid()
    {
        _grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        _grid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(0xF7, 0xFA, 0xFC));
        _grid.RowHeaderWidth = 0;
        _grid.SelectionMode = DataGridSelectionMode.Single;
        _grid.Columns.Add(new DataGridCheckBoxColumn { Header = "🛒", Binding = new Binding(nameof(OrderLineItem.IsClientOrder)), Width = 42 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Qty", Binding = new Binding(nameof(OrderLineItem.Quantity)), Width = 54 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "Product NAME", Binding = new Binding(nameof(OrderLineItem.ProductName)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = "WHS", Binding = new Binding(nameof(OrderLineItem.Whs)), Width = 82 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "RRP", Binding = new Binding(nameof(OrderLineItem.Rrp)), Width = 82 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "% Profit", Binding = new Binding(nameof(OrderLineItem.ProfitPercent)), Width = 90 });
    }

    private void RefreshMatches()
    {
        var query = _search.Text.Trim();
        _matches.Items.Clear();
        if (string.IsNullOrWhiteSpace(query))
        {
            UpdateProfitVisual();
            return;
        }

        foreach (var product in _products
                .Select(product => new { Product = product, Score = Score(product.Name, query) })
                .Where(match => match.Score > 0)
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Product.Name)
                .Take(12)
                .Select(match => match.Product))
        {
            _matches.Items.Add(new ListBoxItem
            {
                Tag = product,
                Content = BuildHighlightedResult(product, query),
                Padding = new Thickness(8, 5, 8, 5)
            });
        }

        if (_matches.Items.Count > 0)
        {
            _matches.SelectedIndex = 0;
        }
    }

    private void AddBestOrCustom()
    {
        if (_matches.Items.Count > 0)
        {
            _matches.SelectedIndex = 0;
            AddSelectedMatch();
            return;
        }

        var custom = _search.Text.Trim();
        if (custom.Length == 0)
        {
            return;
        }

        var customName = custom.ToUpperInvariant();
        AddItem(new OrderLineItem { ProductName = customName, IsCustom = true }, QuantityDialog.Ask(this, customName));
    }

    private void AddSelectedMatch()
    {
        if (SelectedProduct() is ProductReference product)
        {
            var quantity = QuantityDialog.Ask(this, product.Name);
            if (quantity <= 0)
            {
                return;
            }

            AddItem(new OrderLineItem
            {
                ProductName = product.Name,
                Whs = product.Whs,
                Rrp = product.Rrp,
                ProfitPercent = product.ProfitPercent
            }, quantity);
        }
    }

    private void AddItem(OrderLineItem item, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        var existing = _items.FirstOrDefault(line => line.ProductName.Equals(item.ProductName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Quantity += quantity;
            _grid.Items.Refresh();
        }
        else
        {
            item.Quantity = quantity;
            _items.Add(item);
        }

        _search.Clear();
        Persist();
    }

    private void ToggleClientOrder()
    {
        if (_grid.SelectedItem is OrderLineItem item)
        {
            item.IsClientOrder = !item.IsClientOrder;
            _grid.Items.Refresh();
            Persist();
        }
    }

    private void ChangeQty(int delta)
    {
        if (_grid.SelectedItem is not OrderLineItem item)
        {
            return;
        }

        item.Quantity = Math.Max(1, item.Quantity + delta);
        _grid.Items.Refresh();
        Persist();
    }

    private void DeleteSelected()
    {
        if (_grid.SelectedItem is OrderLineItem item)
        {
            _items.Remove(item);
            Persist();
        }
    }

    private void ClearList()
    {
        if (MessageBox.Show("Delete the current order list?", "Futuristic HUD", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _items.Clear();
        Persist();
    }

    private void Persist()
    {
        _settings.Presets.OrderItems = _items.ToList();
        _settings.Save();
    }

    public void SaveState()
    {
        Persist();
    }

    private void PrintList()
    {
        Persist();
        if (ReceiptPreviewWindow.ShowIfEnabled(
                _settings.Presets,
                "Order List Preview",
                () => BuildPrintVisual(420),
                PrintListDirect))
        {
            return;
        }

        PrintListDirect();
    }

    private void PrintListDirect()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var panel = BuildPrintVisual(Math.Min(dialog.PrintableAreaWidth, 420));
        ReceiptPreviewWindow.PrintVisual(dialog, panel, "Order List");
    }

    private FrameworkElement BuildPrintVisual(double width)
    {
        var panel = new StackPanel { Width = width, Background = Brushes.White, Margin = new Thickness(0) };
        panel.Children.Add(new TextBlock { Text = $"Order List {DateTime.Now:dd-MMM-yy}", FontSize = 18, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(8) });
        panel.Children.Add(PrintRow("Qty", "Product", "%", isHeader: true, highlight: false));
        foreach (var item in _items)
        {
            panel.Children.Add(PrintRow(item.Quantity.ToString(CultureInfo.InvariantCulture), item.ProductName, item.ProfitPercent, isHeader: false, highlight: item.IsClientOrder));
        }
        return panel;
    }

    private ProductReference? SelectedProduct() => (_matches.SelectedItem as ListBoxItem)?.Tag as ProductReference;

    private void UpdateProfitVisual()
    {
        var percentText = (_grid.SelectedItem as OrderLineItem)?.ProfitPercent ?? SelectedProduct()?.ProfitPercent ?? "";
        var percent = ParsePercent(percentText);
        _profitText.Text = percentText.Length == 0 ? "🍼 Profit margin: -" : $"🍼 Profit margin: {percentText}%";
        _profitBar.Value = percent;
        _profitBar.Foreground = new SolidColorBrush(percent >= 50 ? Color.FromRgb(0x22, 0xC5, 0x5E) : percent >= 25 ? Color.FromRgb(0xF5, 0x9E, 0x0B) : Color.FromRgb(0xEF, 0x44, 0x44));
    }

    private static double ParsePercent(string value)
    {
        var cleaned = value.Replace("%", "", StringComparison.Ordinal).Trim();
        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? Math.Clamp(number, 0, 100)
            : 0;
    }

    private static TextBlock BuildHighlightedResult(ProductReference product, string query)
    {
        var block = new TextBlock { FontSize = 15, TextWrapping = TextWrapping.Wrap };
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        AddHighlightedRuns(block, product.Name, tokens);
        block.Inlines.Add(new Run($"   WHS {product.Whs}   RRP {product.Rrp}   {product.ProfitPercent}%") { Foreground = Brushes.DimGray });
        return block;
    }

    private static void AddHighlightedRuns(TextBlock block, string text, string[] tokens)
    {
        var index = 0;
        while (index < text.Length)
        {
            var next = tokens
                .Select(token => new { Token = token, Index = text.IndexOf(token, index, StringComparison.OrdinalIgnoreCase) })
                .Where(match => match.Index >= 0)
                .OrderBy(match => match.Index)
                .FirstOrDefault();
            if (next is null)
            {
                block.Inlines.Add(new Run(text[index..]));
                return;
            }

            if (next.Index > index)
            {
                block.Inlines.Add(new Run(text[index..next.Index]));
            }

            block.Inlines.Add(new Run(text.Substring(next.Index, next.Token.Length)) { FontWeight = FontWeights.Bold });
            index = next.Index + next.Token.Length;
        }
    }

    private static Grid PrintRow(string qty, string product, string profit, bool isHeader, bool highlight)
    {
        var row = new Grid
        {
            Background = highlight ? new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xDE)) : Brushes.White,
            Margin = new Thickness(4, 0, 4, 0)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        AddPrintCell(row, qty, 0, isHeader);
        AddPrintCell(row, product, 1, isHeader);
        AddPrintCell(row, profit, 2, isHeader);
        return row;
    }

    private static void AddPrintCell(Grid row, string text, int column, bool isHeader)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = isHeader ? 11 : 10,
            FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(3, 2, 3, 2)
        };
        Grid.SetColumn(block, column);
        row.Children.Add(block);
    }

    private static int Score(string name, string query)
    {
        var nameUpper = name.ToUpperInvariant();
        var tokens = query.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var score = 0;
        foreach (var token in tokens)
        {
            var index = nameUpper.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
            {
                return 0;
            }

            score += index == 0 ? 20 : 10;
        }

        return score - Math.Min(nameUpper.Length, 80) / 20;
    }

    private static List<ProductReference> LoadProducts(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return [];
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            return [];
        }

        var delimiter = lines[0].Contains('\t') ? '\t' : ',';
        var headers = lines[0].Split(delimiter).Select(header => header.Trim().ToUpperInvariant()).ToArray();
        var nameIndex = Array.FindIndex(headers, header => header.Contains("PRODUCT") && header.Contains("NAME"));
        var whsIndex = Array.FindIndex(headers, header => header == "WHS");
        var rrpIndex = Array.FindIndex(headers, header => header == "RRP");
        var profitIndex = Array.FindIndex(headers, header => header == "%");
        var products = new List<ProductReference>();
        foreach (var line in lines.Skip(1))
        {
            var cells = line.Split(delimiter);
            string Cell(int index) => index >= 0 && index < cells.Length ? cells[index].Trim() : "";
            var name = Cell(nameIndex);
            if (name.Length == 0)
            {
                continue;
            }

            products.Add(new ProductReference(name, Cell(whsIndex), Cell(rrpIndex), Cell(profitIndex)));
        }

        return products;
    }

    private static Button Button(string label, string color, RoutedEventHandler handler, double width = 98)
    {
        var button = new Button
        {
            Content = label,
            Width = width,
            Height = 32,
            Margin = new Thickness(8, 0, 0, 0),
            Background = (Brush)new BrushConverter().ConvertFromString(color)!,
            BorderBrush = Brushes.LightGray
        };
        button.Click += handler;
        return button;
    }
}

public sealed record ProductReference(string Name, string Whs, string Rrp, string ProfitPercent)
{
    public override string ToString() => $"{Name}   WHS {Whs}   RRP {Rrp}   {ProfitPercent}%";
}
