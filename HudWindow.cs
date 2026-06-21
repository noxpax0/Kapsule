using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace FuturisticCtrlHud;

public sealed class HudWindow : Window
{
    private readonly RadialHudControl _hud;
    private bool _isClosing;

    public HudWindow(AppSettings settings)
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var screen = Screen.FromPoint(cursor);
        var center = new System.Windows.Point(
            Math.Clamp(cursor.X - screen.Bounds.Left, HudConfig.HudRadius + 28, screen.Bounds.Width - HudConfig.HudRadius - 28),
            Math.Clamp(cursor.Y - screen.Bounds.Top, HudConfig.HudRadius + 28, screen.Bounds.Height - HudConfig.HudRadius - 28));
        _hud = new RadialHudControl(settings.ToMenuOptions(), center);

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Focusable = true;
        Content = _hud;
        Opacity = 0;

        _hud.ActionRequested += actionKey =>
        {
            HudActions.Run(actionKey);
            CloseHud();
        };
        _hud.CloseRequested += CloseHud;
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                CloseHud();
            }
        };
    }

    public void ShowHud()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var screen = Screen.FromPoint(cursor);
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
        Width = screen.Bounds.Width;
        Height = screen.Bounds.Height;

        Show();
        Activate();
        Focus();

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fade);
    }

    public void CloseHud()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(130))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }
}
