using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace FuturisticCtrlHud;

public sealed class HudController : IDisposable
{
    private readonly GlobalCtrlHook _hook = new();
    private TrayIconManager? _tray;
    private AppSettings _settings = AppSettings.Load();
    private HudWindow? _window;
    private SettingsWindow? _settingsWindow;
    private readonly DispatcherTimer _ctrlSpaceTimer = new() { Interval = TimeSpan.FromMilliseconds(HudConfig.DoubleTapMilliseconds) };

    public void Start()
    {
        _hook.CtrlDoubleTapped += OnCtrlDoubleTapped;
        _hook.CtrlSpaceTapped += OnCtrlSpaceTapped;
        _ctrlSpaceTimer.Tick += (_, _) =>
        {
            _ctrlSpaceTimer.Stop();
            RestartApp();
        };
        _hook.Start();
        _tray = new TrayIconManager(this);
        _tray.Show();
    }

    public void ToggleHud()
    {
        if (_window?.IsVisible == true)
        {
            _window.CloseHud();
            return;
        }

        _settings = AppSettings.Load();
        _window = new HudWindow(_settings);
        _window.ShowHud();
    }

    public void ReloadMenu()
    {
        SaveAndCloseToolWindows();
        if (_window?.IsVisible == true)
        {
            _window.CloseHud();
        }

        _settings = AppSettings.Load();
        _window = new HudWindow(_settings);
        _window.ShowHud();
    }

    public void OpenSettings()
    {
        _settings = AppSettings.Load();
        if (_settingsWindow?.IsVisible == true)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    public void Exit()
    {
        Application.Current.Shutdown();
    }

    public void ResetApp()
    {
        _window?.CloseHud();
        _settingsWindow?.Close();
        var defaults = AppSettings.LoadInstallDefaults();
        defaults.Save();
        MessageBox.Show("HUD settings reset to install defaults.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void RestartApp()
    {
        SaveAndCloseToolWindows();
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                ArgumentList = { "--restart-wait" },
                UseShellExecute = true
            });
        }

        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _hook.CtrlDoubleTapped -= OnCtrlDoubleTapped;
        _hook.CtrlSpaceTapped -= OnCtrlSpaceTapped;
        _hook.Dispose();
        _tray?.Dispose();
        _settingsWindow?.Close();
        _window?.Close();
    }

    private void OnCtrlDoubleTapped()
    {
        Application.Current.Dispatcher.BeginInvoke(ReloadMenu);
    }

    private void OnCtrlSpaceTapped()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (_ctrlSpaceTimer.IsEnabled)
            {
                _ctrlSpaceTimer.Stop();
                Exit();
                return;
            }

            _ctrlSpaceTimer.Start();
        });
    }

    private static void SaveAndCloseToolWindows()
    {
        foreach (var window in Application.Current.Windows.OfType<Window>().ToList())
        {
            if (window is HudWindow)
            {
                continue;
            }

            if (window is IAutoSaveWindow autoSaveWindow)
            {
                autoSaveWindow.SaveState();
            }

            window.Close();
        }
    }
}
