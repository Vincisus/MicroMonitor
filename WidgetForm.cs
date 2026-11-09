using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicroMonitor;

public sealed class TaskbarWidget : Form
{
    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly System.Windows.Forms.Timer _positionTimer;
    private readonly ContextMenuStrip _menu;

    private float? _cpuLoad;
    private float? _cpuTemp;
    private float? _gpuLoad;
    private float? _gpuTemp;
    private float _ramUsedGb;
    private float _ramTotalGb;

    private string _line1 = "CPU --% --°C";
    private string _line2 = "GPU --% --°C   RAM --/--GB";

    private readonly Font _font = new("Segoe UI Semibold", 8.5f, FontStyle.Regular);
    private readonly Color _foreColor = Color.White;
    private IntPtr _parentTaskbar = IntPtr.Zero;

    public TaskbarWidget()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        ForeColor = _foreColor;
        DoubleBuffered = true;

        _menu = new ContextMenuStrip();
        _menu.Items.Add("Esci", null, (_, _) => Close());
        ContextMenuStrip = _menu;

        _updateTimer = new System.Windows.Forms.Timer { Interval = SettingsManager.RefreshIntervalMs };
        _updateTimer.Tick += (_, _) => UpdateMetrics();

        _positionTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _positionTimer.Tick += (_, _) => CheckLayoutSync();

        UpdateMetrics();
        AttachToTaskbar();

        _updateTimer.Start();
        _positionTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return cp;
        }
    }

    private void AttachToTaskbar()
    {
        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero)
        {
            _parentTaskbar = taskbar;
            SetParent(Handle, taskbar);
            UpdateBoundsInsideTaskbar();
        }
    }

    private void UpdateMetrics()
    {
        try
        {
            var values = HardwareMonitor.Read();
            _cpuLoad = values.CpuLoad;
            _cpuTemp = values.CpuTemp;
            _gpuLoad = values.GpuLoad;
            _gpuTemp = values.GpuTemp;

            MemoryMonitor.GetMemory(out _ramUsedGb, out _ramTotalGb);

            string cpuTempStr = _cpuTemp.HasValue ? $"{_cpuTemp.Value:F0}°C" : "--°C";
            string gpuTempStr = _gpuTemp.HasValue ? $"{_gpuTemp.Value:F0}°C" : "--°C";

            _line1 = $"CPU {_cpuLoad ?? 0:F0}%  {cpuTempStr}";
            _line2 = $"GPU {_gpuLoad ?? 0:F0}%  {gpuTempStr}   RAM {_ramUsedGb:F1}/{_ramTotalGb:F0}GB";

            UpdateBoundsInsideTaskbar();
            Invalidate();
        }
        catch
        {
            _line1 = "MicroMonitor";
            _line2 = "";
        }
    }

    private void CheckLayoutSync()
    {
        if (_parentTaskbar == IntPtr.Zero || !IsWindow(_parentTaskbar))
        {
            AttachToTaskbar();
            return;
        }

        UpdateBoundsInsideTaskbar();
    }

    private void UpdateBoundsInsideTaskbar()
    {
        if (_parentTaskbar == IntPtr.Zero) return;

        IntPtr trayNotify = FindWindowEx(_parentTaskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero || !GetWindowRect(trayNotify, out RECT trayScreenRect)) return;

        POINT pt = new POINT { X = trayScreenRect.Left, Y = trayScreenRect.Top };
        ScreenToClient(_parentTaskbar, ref pt);

        GetClientRect(_parentTaskbar, out RECT taskbarClient);

        Size sz1 = TextRenderer.MeasureText(_line1, _font);
        Size sz2 = TextRenderer.MeasureText(_line2, _font);

        int contentWidth = Math.Max(sz1.Width, sz2.Width) + 14;
        int taskbarHeight = taskbarClient.Bottom - taskbarClient.Top;
        int targetHeight = Math.Max(taskbarHeight, 38);

        int targetX = pt.X - contentWidth - 4;
        int targetY = 0;

        SetWindowPos(Handle, IntPtr.Zero, targetX, targetY, contentWidth, targetHeight, 0x0010 | 0x0004); // SWP_NOACTIVATE | SWP_NOZORDER
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        int halfHeight = ClientSize.Height / 2;
        Rectangle r1 = new(0, 2, ClientSize.Width, halfHeight - 1);
        Rectangle r2 = new(0, halfHeight - 1, ClientSize.Width, halfHeight);

        TextRenderer.DrawText(e.Graphics, _line1, _font, r1, _foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, _line2, _font, r2, _foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _updateTimer.Stop();
        _positionTimer.Stop();
        HardwareMonitor.Shutdown();
        base.OnFormClosed(e);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr afterChild, string className, string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr handle, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);
}