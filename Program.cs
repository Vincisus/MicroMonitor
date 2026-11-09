using System;
using System.Threading;
using System.Windows.Forms;

namespace MicroMonitor;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    private static void Main()
    {
        _mutex = new Mutex(true, "MicroMonitor_SingleInstance", out bool isFirstInstance);

        if (!isFirstInstance)
        {
            _mutex.Dispose();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        try
        {
            Application.Run(new TaskbarWidget());
        }
        finally
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}