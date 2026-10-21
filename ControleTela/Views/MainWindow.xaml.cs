using ControleTela.ViewModels;
using System.ComponentModel;
using System.Windows;
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

        // Acompanha o tema claro/escuro e a cor de destaque do Windows enquanto o app está aberto.
        SystemThemeWatcher.Watch(this);

        Activated += (_, _) => ViewModel.RefreshWarnings();
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
