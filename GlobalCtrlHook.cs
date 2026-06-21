using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace FuturisticCtrlHud;

public sealed class GlobalCtrlHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeyup = 0x0105;
    private const int VkControl = 0x11;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkSpace = 0x20;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private DateTime _lastCtrlTap = DateTime.MinValue;
    private bool _ctrlIsDown;
    private bool _spaceIsDown;

    public event Action? CtrlDoubleTapped;
    public event Action? CtrlSpaceTapped;

    public GlobalCtrlHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        _hookId = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(currentModule?.ModuleName), 0);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            var vkCode = Marshal.ReadInt32(lParam);

            if (IsCtrl(vkCode) && (message == WmKeydown || message == WmSyskeydown))
            {
                if (!_ctrlIsDown)
                {
                    _ctrlIsDown = true;
                    var now = DateTime.UtcNow;
                    if ((now - _lastCtrlTap).TotalMilliseconds <= HudConfig.DoubleTapMilliseconds)
                    {
                        _lastCtrlTap = DateTime.MinValue;
                        CtrlDoubleTapped?.Invoke();
                    }
                    else
                    {
                        _lastCtrlTap = now;
                    }
                }
            }
            else if (IsCtrl(vkCode) && (message == WmKeyup || message == WmSyskeyup))
            {
                _ctrlIsDown = false;
            }
            else if (vkCode == VkSpace && _ctrlIsDown && (message == WmKeydown || message == WmSyskeydown))
            {
                if (!_spaceIsDown)
                {
                    _spaceIsDown = true;
                    CtrlSpaceTapped?.Invoke();
                }
            }
            else if (vkCode == VkSpace && (message == WmKeyup || message == WmSyskeyup))
            {
                _spaceIsDown = false;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool IsCtrl(int vkCode) => vkCode is VkControl or VkLControl or VkRControl;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
