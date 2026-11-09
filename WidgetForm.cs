using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MicroMonitor;

public sealed class TaskbarWidget : Form
{
    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly System.Windows.Forms.Timer _syncTimer;
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

    public TaskbarWidget()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        ForeColor = _foreColor;
        DoubleBuffered = true;
        TopMost = true;

        _menu = new ContextMenuStrip();
        _menu.Items.Add("Esci", null, (_, _) => Close());
        ContextMenuStrip = _menu;

        _updateTimer = new System.Windows.Forms.Timer { Interval = SettingsManager.RefreshIntervalMs };
        _updateTimer.Tick += (_, _) => UpdateMetrics();

        _syncTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _syncTimer.Tick += (_, _) => SyncPosition();

        UpdateMetrics();
        SyncPosition();

        _updateTimer.Start();
        _syncTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return cp;
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

            Invalidate();
        }
        catch
        {
            _line1 = "MicroMonitor";
            _line2 = "";
        }
    }

    private void SyncPosition()
    {
        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || !IsWindowVisible(taskbar))
        {
            if (Visible) Hide();
            return;
        }

        IntPtr trayNotify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero)
        {
            if (Visible) Hide();
            return;
        }

        if (!GetWindowRect(trayNotify, out RECT trayRect) || !GetWindowRect(taskbar, out RECT taskbarRect))
        {
            if (Visible) Hide();
            return;
        }

        var screen = Screen.FromHandle(taskbar).Bounds;

        bool isHidden = taskbarRect.Top >= screen.Bottom - 4 ||
                        taskbarRect.Bottom <= screen.Top + 4 ||
                        trayRect.Right <= trayRect.Left;

        if (isHidden)
        {
            if (Visible) Hide();
            return;
        }

        Size sz1 = TextRenderer.MeasureText(_line1, _font);
        Size sz2 = TextRenderer.MeasureText(_line2, _font);
        int contentWidth = Math.Max(sz1.Width, sz2.Width) + 14;

        int taskbarHeight = taskbarRect.Bottom - taskbarRect.Top;
        int widgetHeight = Math.Max(taskbarHeight, 38);

        int targetX = trayRect.Left - contentWidth - 4;
        int targetY = taskbarRect.Top;

        SetWindowPos(
            Handle,
            new IntPtr(-1), // HWND_TOPMOST
            targetX,
            targetY,
            contentWidth,
            widgetHeight,
            0x0010 | 0x0040); // SWP_NOACTIVATE | SWP_SHOWWINDOW

        if (!Visible) Show();
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
        _syncTimer.Stop();
        HardwareMonitor.Shutdown();
        base.OnFormClosed(e);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr afterChild, string className, string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);
}