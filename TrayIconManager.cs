using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace FuturisticCtrlHud;

public sealed class TrayIconManager : IDisposable
{
    private readonly HudController _controller;
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconManager(HudController controller)
    {
        _controller = controller;
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Futuristic Ctrl HUD",
            ContextMenuStrip = BuildMenu(),
            Visible = false
        };
        _notifyIcon.DoubleClick += (_, _) => _controller.ToggleHud();
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show HUD", null, (_, _) => _controller.ToggleHud());
        menu.Items.Add("Settings", null, (_, _) => _controller.OpenSettings());
        menu.Items.Add("Restart App", null, (_, _) => _controller.RestartApp());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _controller.Exit());
        return menu;
    }

    private static Icon LoadIcon()
    {
        var logoPath = AppSettings.Load().Presets.LogoPath;
        if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
        {
            try
            {
                using var source = new Bitmap(logoPath);
                using var resized = new Bitmap(source, new Size(32, 32));
                var handle = resized.GetHicon();
                var icon = (Icon)Icon.FromHandle(handle).Clone();
                DestroyIcon(handle);
                return icon;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        return SystemIcons.Application;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
