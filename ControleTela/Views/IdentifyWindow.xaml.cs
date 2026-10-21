using ControleTela.Models;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ControleTela.Views;

/// <summary>Mostra o número do monitor no canto inferior esquerdo dele, como o "Identificar" do Windows.</summary>
public partial class IdentifyWindow : Window
{
    private const int MarginPx = 48;
    private readonly MonitorInfo _monitor;

    public IdentifyWindow(MonitorInfo monitor)
    {
        _monitor = monitor;
        InitializeComponent();
        NumberText.Text = monitor.Number.ToString();
        Frame.BorderBrush = MonitorMapControl.GetWindowsAccentBrush();
        Loaded += (_, _) => PlaceOnMonitor();
        // Ao entrar num monitor com outra escala (DPI), o WPF redimensiona a janela: reposicionamos.
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(PlaceOnMonitor, DispatcherPriority.Render);
    }

    /// <summary>Abre uma janela por monitor e fecha todas depois de 2 segundos.</summary>
    public static void ShowFor(IEnumerable<MonitorInfo> monitors)
    {
        var windows = monitors.Select(m => new IdentifyWindow(m)).ToList();
        foreach (var w in windows) w.Show();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            foreach (var w in windows) w.Close();
        };
        timer.Start();
    }

    private void PlaceOnMonitor()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        GetWindowRect(hwnd, out var rect);
        int height = rect.Bottom - rect.Top;
        SetWindowPos(hwnd, HWND_TOPMOST,
            _monitor.X + MarginPx, _monitor.Y + _monitor.Height - height - MarginPx,
            0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
        Opacity = 1;
    }

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
