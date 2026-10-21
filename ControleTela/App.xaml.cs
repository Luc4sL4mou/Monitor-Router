using ControleTela.Services;
using ControleTela.ViewModels;
using ControleTela.Views;
using Microsoft.Win32;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Wpf.Ui.Appearance;

namespace ControleTela;

/// <summary>
/// Ponto de entrada do app: cria o ViewModel, a janela e o ícone da bandeja, escuta mudanças
/// de monitores e garante que as cores originais voltem ao sair (por qualquer caminho).
/// </summary>
public partial class App : Application
{
    private MainViewModel _viewModel = null!;
    private MainWindow _window = null!;
    private TrayIcon? _tray;
    private DispatcherTimer? _displayChangeTimer;
    private bool _exiting;
    private bool _trayTipShown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Faz o StringFormat dos bindings usar o formato brasileiro (ex.: "1,20" e "80%").
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        ApplicationThemeManager.ApplySystemTheme(); // claro/escuro + cor de destaque do Windows

        _viewModel = new MainViewModel();
        _viewModel.Initialize();
        _window = new MainWindow(_viewModel);
        _window.IsVisibleChanged += (_, _) =>
        {
            // Na primeira vez que minimizar, explica onde o app foi parar.
            if (!_window.IsVisible && !_exiting && !_trayTipShown)
            {
                _trayTipShown = true;
                _tray?.ShowTip("Monitor Router continua ativo", "Os ajustes seguem aplicados. Clique aqui no ícone para abrir.");
            }
        };

        _tray = new TrayIcon(
            open: () => _window.ShowFromTray(),
            resetAll: () => _viewModel.ResetAllCommand.Execute(null),
            presetNames: _viewModel.GetPresetNames,
            loadPreset: name => _viewModel.LoadPreset(name),
            exit: () => ExitApplication());

        // Rede de segurança: mesmo se o processo for encerrado de outro jeito, restaura as cores.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _viewModel.Shutdown();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        if (!e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase))
            _window.Show();
    }

    /// <summary>
    /// O Windows pode disparar vários eventos seguidos e resetar a Gamma Ramp. Esperamos 500 ms
    /// sem novos eventos ("debounce") e então relemos os monitores e reaplicamos tudo.
    /// </summary>
    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_displayChangeTimer is null)
            {
                _displayChangeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _displayChangeTimer.Tick += (_, _) =>
                {
                    _displayChangeTimer.Stop();
                    if (!_exiting) _viewModel.RefreshMonitors();
                };
            }
            _displayChangeTimer.Stop();
            _displayChangeTimer.Start();
        });
    }

    /// <summary>
    /// Saída "oficial": restaura as cores, some com o ícone da bandeja e encerra.
    /// fromWindow = true quando veio do X da janela (ela já está fechando; não chamamos Close de novo).
    /// </summary>
    public void ExitApplication(bool fromWindow = false)
    {
        if (_exiting) return;
        _exiting = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _viewModel.Shutdown();
        _tray?.Dispose();
        _tray = null;
        if (!fromWindow) _window.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Shutdown();
        _tray?.Dispose();
        base.OnExit(e);
    }
}
