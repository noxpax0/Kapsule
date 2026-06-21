using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace FuturisticCtrlHud;

public sealed class RadialHudControl : FrameworkElement
{
    private int _hoverIndex = -1;
    private WpfPoint _center;
    private readonly MenuOption[] _options;

    public event Action<string>? ActionRequested;
    public event Action? CloseRequested;

    public RadialHudControl(MenuOption[] options, WpfPoint center)
    {
        _options = options;
        _center = center;
        Focusable = true;
        MouseMove += OnMouseMove;
        MouseLeave += (_, _) =>
        {
            _hoverIndex = -1;
            InvalidateVisual();
        };
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(52, 2, 8, 14)), null, new Rect(0, 0, ActualWidth, ActualHeight));
        DrawOuterGlow(dc);
        DrawSegments(dc);
        DrawCenter(dc);
        DrawTicks(dc);
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var index = IndexAt(e.GetPosition(this));
        if (index != _hoverIndex)
        {
            _hoverIndex = index;
            InvalidateVisual();
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var index = IndexAt(e.GetPosition(this));
        if (index < 0)
        {
            CloseRequested?.Invoke();
            return;
        }

        ActionRequested?.Invoke(_options[index].ActionKey);
    }

    private void DrawOuterGlow(DrawingContext dc)
    {
        var radius = HudConfig.HudRadius;
        var outerRect = RectAround(_center, radius + 22);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 38, 217, 255)), 2.5), _center, outerRect.Width / 2, outerRect.Height / 2);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 159, 47)), 5), _center, radius + 10, radius + 10);
    }

    private void DrawSegments(DrawingContext dc)
    {
        var options = _options;
        if (options.Length == 0)
        {
            return;
        }

        var sweep = 360.0 / options.Length;
        var gap = 4.0;
        var startOffset = -90 - sweep / 2;

        for (var i = 0; i < options.Length; i++)
        {
            var option = options[i];
            var start = startOffset + i * sweep + gap / 2;
            var span = sweep - gap;
            var geometry = CreateSliceGeometry(start, span, HudConfig.InnerRadius, HudConfig.HudRadius);
            var hovered = i == _hoverIndex;

            var fillAlpha = hovered ? (byte)150 : (byte)96;
            var edgeAlpha = hovered ? (byte)255 : (byte)168;
            var fill = new SolidColorBrush(Color.FromArgb(fillAlpha, 5, 22, 32));
            var edge = new SolidColorBrush(Color.FromArgb(edgeAlpha, option.Accent.R, option.Accent.G, option.Accent.B));

            dc.DrawGeometry(fill, new Pen(edge, hovered ? 2.4 : 1.25), geometry);

            if (hovered)
            {
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(70, option.Accent.R, option.Accent.G, option.Accent.B)), 10), geometry);
            }

            DrawLabel(dc, option.Label, start + span / 2, option.Accent, hovered);
        }
    }

    private void DrawCenter(DrawingContext dc)
    {
        var inner = HudConfig.InnerRadius;
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(194, 3, 12, 18)), new Pen(new SolidColorBrush(Color.FromArgb(220, 38, 217, 255)), 2), _center, inner, inner);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(205, 255, 159, 47)), 1.5), _center, inner - 17, inner - 17);

        var centerText = CenterText();
        var text = new FormattedText(
            centerText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            _hoverIndex >= 0 ? 10 : 15,
            new SolidColorBrush(Color.FromArgb(235, 190, 242, 255)),
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = inner * 1.55,
            MaxTextHeight = inner * 1.6,
            LineHeight = _hoverIndex >= 0 ? 12 : 18
        };
        dc.DrawText(text, new Point(_center.X - text.Width / 2, _center.Y - text.Height / 2));
    }

    private string CenterText()
    {
        if (_hoverIndex < 0 || _hoverIndex >= _options.Length)
        {
            return "CTRL\nHUD";
        }

        var option = _options[_hoverIndex];
        var definition = HudConfig.AvailableActions.FirstOrDefault(action => action.Key == option.ActionKey);
        return definition is null
            ? option.Label
            : $"{option.Label}\n{definition.Description}";
    }

    private void DrawTicks(DrawingContext dc)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(118, 105, 232, 255)), 1);
        for (var tick = 0; tick < 48; tick++)
        {
            var angle = DegreesToRadians(tick * 7.5 - 90);
            var inner = HudConfig.HudRadius + (tick % 4 == 0 ? 7 : 0);
            var outer = HudConfig.HudRadius + (tick % 4 == 0 ? 20 : 14);
            dc.DrawLine(pen, Polar(angle, inner), Polar(angle, outer));
        }
    }

    private void DrawLabel(DrawingContext dc, string label, double degrees, MediaColor accent, bool hovered)
    {
        var angle = DegreesToRadians(degrees);
        var labelRadius = HudConfig.InnerRadius + (HudConfig.HudRadius - HudConfig.InnerRadius) * 0.58;
        var point = Polar(angle, labelRadius);
        var color = hovered ? accent : MediaColor.FromRgb(207, 246, 255);
        var text = new FormattedText(
            label,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            label.Length < 12 ? 14 : 12,
            new SolidColorBrush(color),
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = 104,
            MaxTextHeight = 38,
            LineHeight = 15
        };
        dc.DrawText(text, new Point(point.X - text.Width / 2, point.Y - text.Height / 2));
    }

    private StreamGeometry CreateSliceGeometry(double startDegrees, double spanDegrees, double innerRadius, double outerRadius)
    {
        var start = DegreesToRadians(startDegrees);
        var end = DegreesToRadians(startDegrees + spanDegrees);
        var outerStart = Polar(start, outerRadius);
        var outerEnd = Polar(end, outerRadius);
        var innerEnd = Polar(end, innerRadius);
        var innerStart = Polar(start, innerRadius);

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(outerStart, true, true);
        ctx.ArcTo(outerEnd, new Size(outerRadius, outerRadius), 0, spanDegrees > 180, SweepDirection.Clockwise, true, false);
        ctx.LineTo(innerEnd, true, false);
        ctx.ArcTo(innerStart, new Size(innerRadius, innerRadius), 0, spanDegrees > 180, SweepDirection.Counterclockwise, true, false);
        geometry.Freeze();
        return geometry;
    }

    private int IndexAt(WpfPoint point)
    {
        var dx = point.X - _center.X;
        var dy = point.Y - _center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < HudConfig.InnerRadius || distance > HudConfig.HudRadius)
        {
            return -1;
        }

        var count = _options.Length;
        if (count == 0)
        {
            return -1;
        }

        var angle = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
        var sweep = 360.0 / count;
        var adjusted = (angle + 90 + sweep / 2) % 360;
        return (int)(adjusted / sweep) % count;
    }

    private WpfPoint Polar(double radians, double radius) => new(_center.X + Math.Cos(radians) * radius, _center.Y + Math.Sin(radians) * radius);

    private static Rect RectAround(WpfPoint center, double radius) => new(center.X - radius, center.Y - radius, radius * 2, radius * 2);

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
