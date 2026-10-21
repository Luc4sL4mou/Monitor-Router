using ControleTela.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace ControleTela.Views;

/// <summary>
/// "Code-behind" da janela: só o que é puramente visual (identificar, minimizar para a bandeja).
/// A lógica de verdade fica no MainViewModel.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        FitToCurrentMonitor();

        // Acompanha o tema claro/escuro e a cor de destaque do Windows enquanto o app está aberto.
        SystemThemeWatcher.Watch(this);

        Activated += (_, _) => ViewModel.RefreshWarnings();
    }

    /// <summary>
    /// Abre centralizada no monitor onde está o mouse, mas nunca maior que a área útil dele
    /// (sem isso, num monitor de 768 px de altura o topo da janela ficava para fora da tela).
    /// </summary>
    private void FitToCurrentMonitor()
    {
        var area = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Control.MousePosition).WorkingArea;
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX; // pixels físicos -> unidades do WPF
        double areaW = area.Width / scale, areaH = area.Height / scale;

        Width = Math.Min(Width, areaW - 16);
        Height = Math.Min(Height, areaH - 16);
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = area.Left / scale + (areaW - Width) / 2;
        Top = area.Top / scale + (areaH - Height) / 2;
    }

    private void Identify_Click(object sender, RoutedEventArgs e) =>
        IdentifyWindow.ShowFor(ViewModel.Monitors.Select(m => m.Info));

    /// <summary>Minimizar esconde a janela; o ícone na bandeja traz de volta.</summary>
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            ViewModel.SaveState();
        }
    }

    /// <summary>Fechar = sair do app (restaurando as cores). O App cuida disso.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        ((App)System.Windows.Application.Current).ExitApplication(fromWindow: true);
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
