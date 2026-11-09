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

    private string _text = "Avvio...";
    private readonly Font _font = new("Segoe UI", 9f, FontStyle.Regular);
    private readonly Color _backColor = Color.FromArgb(32, 32, 32);
    private readonly Color _foreColor = Color.White;

    public TaskbarWidget()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = _backColor;
        ForeColor = _foreColor;
        Font = _font;
        DoubleBuffered = true;
        TopMost = true;

        _menu = new ContextMenuStrip();
        _menu.Items.Add("Esci", null, (_, _) => Close());
        ContextMenuStrip = _menu;

        _updateTimer = new System.Windows.Forms.Timer { Interval = SettingsManager.RefreshIntervalMs };
        _updateTimer.Tick += (_, _) => UpdateMetrics();

        _positionTimer = new System.Windows.Forms.Timer { Interval = 200 };
        _positionTimer.Tick += (_, _) => UpdatePosition();

        UpdateMetrics();
        UpdatePosition();

        _updateTimer.Start();
        _positionTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000080;
            cp.ExStyle |= 0x00000008;
            cp.ExStyle |= 0x08000000;
            return cp;
        }
    }

    private void UpdateMetrics()
    {
        try
        {
            var values = HardwareMonitor.Read();
            _cpuLoad = values.CpuLoad;
            _cpuTemp = values.CpuTemperature;
            _gpuLoad = values.GpuLoad;
            _gpuTemp = values.GpuTemperature;

            MemoryMonitor.GetMemory(out _ramUsedGb, out _ramTotalGb);

            string cpuTemp = _cpuTemp.HasValue ? $" {_cpuTemp.Value:F0}°C" : "";
            string gpuTemp = _gpuTemp.HasValue ? $" {_gpuTemp.Value:F0}°C" : "";

            _text = $"CPU {_cpuLoad ?? 0:F0}%{cpuTemp}   " +
                    $"RAM {_ramUsedGb:F1}/{_ramTotalGb:F0}GB   " +
                    $"GPU {_gpuLoad ?? 0:F0}%{gpuTemp}";

            ResizeToFit();
            Invalidate();
        }
        catch
        {
            _text = "MicroMonitor";
        }
    }

    private void UpdatePosition()
    {
        IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            Hide();
            return;
        }

        IntPtr trayNotify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (trayNotify == IntPtr.Zero || !IsWindowVisible(taskbar))
        {
            Hide();
            return;
        }

        if (!GetWindowRect(trayNotify, out RECT trayRect))
        {
            Hide();
            return;
        }

        var screen = Screen.FromHandle(Handle).Bounds;

        bool taskbarHidden = trayRect.Left >= screen.Right || trayRect.Right <= screen.Left ||
                             trayRect.Top >= screen.Bottom || trayRect.Bottom <= screen.Top;

        if (taskbarHidden || trayRect.Width <= 0)
        {
            Hide();
            return;
        }

        int height = trayRect.Bottom - trayRect.Top;
        int width = TextRenderer.MeasureText(_text, _font).Width + 16;
        int left = trayRect.Left - width - 4;
        int top = trayRect.Top;

        var target = new Rectangle(left, top, width, height);

        if (Bounds != target)
        {
            SetWindowPos(Handle, IntPtr.Zero, target.Left, target.Top, target.Width, target.Height,
                0x0001 | 0x0002 | 0x0010 | 0x0040);
        }

        if (!Visible)
        {
            Show();
        }
    }

    private void ResizeToFit()
    {
        int width = TextRenderer.MeasureText(_text, _font).Width + 16;
        int height = ClientSize.Height;

        if (width != ClientSize.Width)
        {
            ClientSize = new Size(width, height);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var background = new SolidBrush(_backColor);
        using var foreground = new SolidBrush(_foreColor);

        e.Graphics.FillRectangle(background, ClientRectangle);

        var textSize = TextRenderer.MeasureText(_text, _font);
        var position = new Point(
            (ClientSize.Width - textSize.Width) / 2,
            (ClientSize.Height - textSize.Height) / 2);

        TextRenderer.DrawText(e.Graphics, _text, _font, position, _foreColor);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _updateTimer.Stop();
        _positionTimer.Stop();
        HardwareMonitor.Shutdown();
        base.OnFormClosed(e);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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