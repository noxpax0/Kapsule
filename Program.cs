using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace FuturisticCtrlHud;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "FuturisticCtrlHud.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            if (args.Any(arg => arg.Equals("--restart-wait", StringComparison.OrdinalIgnoreCase)))
            {
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    Thread.Sleep(200);
                    using var retry = new Mutex(true, "FuturisticCtrlHud.SingleInstance", out var retryCreated);
                    if (!retryCreated)
                    {
                        continue;
                    }

                    RunApp(args);
                    return;
                }
            }

            MessageBox.Show("Futuristic Ctrl HUD is already running.", "Futuristic HUD", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        RunApp(args);
    }

    private static void RunApp(string[] args)
    {
        var settings = AppSettings.Load();
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        UiStyles.Apply(app);

        if (Array.Exists(args, arg => arg.Equals("--test-dda-print-visual", StringComparison.OrdinalIgnoreCase)))
        {
            DdaSlipWindow.RunPrintVisualSelfTest();
            return;
        }

        using var controller = new HudController(settings);
        controller.Start();

        if (Array.Exists(args, arg => arg.Equals("--show", StringComparison.OrdinalIgnoreCase) || arg.Equals("-Show", StringComparison.OrdinalIgnoreCase)))
        {
            app.Dispatcher.BeginInvoke(() => controller.ToggleHud());
        }

        if (Array.Exists(args, arg => arg.Equals("--open-dda", StringComparison.OrdinalIgnoreCase)))
        {
            app.Dispatcher.BeginInvoke(() => new DdaSlipWindow(settings).Show());
        }

        app.Run();
    }
}
