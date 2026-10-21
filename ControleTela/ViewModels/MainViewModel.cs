using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControleTela.Models;
using ControleTela.Services;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ControleTela.ViewModels;

/// <summary>Uma opção do menu "Copiar para...": um monitor específico ou "Todos os outros" (Monitor = null).</summary>
public sealed record CopyTarget(string Name, MonitorViewModel? Monitor);

/// <summary>
/// Cérebro da janela principal: lista de monitores, monitor selecionado, comandos dos botões
/// (Redefinir, Copiar, Perfis) e mensagens de aviso. A janela só "liga" (binding) nestas propriedades.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly MonitorService _monitorService = new();
    private readonly GammaRampService _gammaService = new();
    private readonly PresetService _presetService = new();
    private List<Preset> _presets = new();
    private AppState _state = new();
    private bool _isShutDown;
    private bool _loading;

    public ObservableCollection<MonitorViewModel> Monitors { get; } = new();
    public ObservableCollection<CopyTarget> CopyTargets { get; } = new();
    public ObservableCollection<string> PresetNames { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private MonitorViewModel? selectedMonitor;

    public bool HasSelection => SelectedMonitor is not null;

    [ObservableProperty] private CopyTarget? selectedCopyTarget;
    [ObservableProperty] private string? selectedPreset;
    [ObservableProperty] private string newPresetName = "";

    [ObservableProperty] private bool isNightLightOn;

    // Barra de mensagens (InfoBar) do topo.
    [ObservableProperty] private bool isStatusOpen;
    [ObservableProperty] private string statusTitle = "";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private InfoBarSeverity statusSeverity = InfoBarSeverity.Informational;

    [ObservableProperty] private bool startWithWindows;

    public void Initialize()
    {
        _presets = _presetService.LoadPresets();
        _state = _presetService.LoadState();
        RefreshPresetNames();
        _loading = true; // só lê o registro; não queremos regravá-lo agora
        StartWithWindows = StartupService.IsEnabled();
        _loading = false;
        RefreshMonitors();
    }

    /// <summary>
    /// (Re)lê os monitores do Windows. Chamado ao abrir e quando a configuração de vídeo muda
    /// (monitor ligado/desligado, resolução, posição). Monitores que já conhecíamos mantêm os
    /// ajustes; os novos recebem o último estado salvo. No fim, reaplica tudo, porque o Windows
    /// costuma resetar a Gamma Ramp nessas mudanças.
    /// </summary>
    [RelayCommand]
    public void RefreshMonitors()
    {
        var previousId = SelectedMonitor?.StableId;
        var infos = _monitorService.GetActiveMonitors();
        var hdr = HdrService.GetHdrDisplays();

        var existing = Monitors.ToDictionary(m => m.StableId);
        var updated = new List<MonitorViewModel>();
        foreach (var info in infos)
        {
            if (existing.Remove(info.StableId, out var vm))
            {
                vm.UpdateInfo(info);
            }
            else
            {
                var saved = _state.Monitors.GetValueOrDefault(info.StableId) ?? new ColorSettings();
                vm = new MonitorViewModel(info, saved);
                vm.SettingsChanged += OnMonitorSettingsChanged;
            }
            vm.IsHdr = hdr.Contains(info.DeviceName);
            updated.Add(vm);
        }
        foreach (var gone in existing.Values) gone.SettingsChanged -= OnMonitorSettingsChanged;

        Monitors.Clear();
        foreach (var vm in updated) Monitors.Add(vm);

        SelectedMonitor = Monitors.FirstOrDefault(m => m.StableId == previousId)
                          ?? Monitors.FirstOrDefault(m => m.Info.IsPrimary)
                          ?? Monitors.FirstOrDefault();

        foreach (var vm in Monitors) ApplyMonitor(vm);
        RefreshWarnings();
    }

    /// <summary>Reconfere a Luz noturna (barato; chamado quando a janela ganha foco).</summary>
    public void RefreshWarnings() => IsNightLightOn = NightLightService.IsEnabled();

    partial void OnSelectedMonitorChanged(MonitorViewModel? value) => RebuildCopyTargets();

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (_loading) return;
        try { StartupService.SetEnabled(value); }
        catch (Exception ex) { ShowStatus(InfoBarSeverity.Error, "Não foi possível alterar a inicialização", ex.Message); }
    }

    private void OnMonitorSettingsChanged(object? sender, EventArgs e)
    {
        if (sender is MonitorViewModel vm) ApplyMonitor(vm);
    }

    /// <summary>
    /// Envia a rampa para o monitor. Se tudo estiver no padrão, devolvemos a rampa ORIGINAL
    /// (assim não apagamos uma calibração ou a Luz noturna à toa).
    /// </summary>
    private void ApplyMonitor(MonitorViewModel vm)
    {
        var settings = vm.ToSettings();
        var result = settings.IsDefault
            ? _gammaService.Restore(vm.Info)
            : _gammaService.Apply(vm.Info, settings);

        if (!result.Success)
            ShowStatus(InfoBarSeverity.Error, $"Não foi possível ajustar a {vm.DisplayName}", result.Message ?? "");
        else if (StatusSeverity == InfoBarSeverity.Error && IsStatusOpen)
            IsStatusOpen = false; // o erro anterior foi resolvido
    }

    // ---------- Redefinir ----------

    [RelayCommand]
    private void Reset() => SelectedMonitor?.Load(new ColorSettings());

    [RelayCommand]
    private void ResetAll()
    {
        foreach (var vm in Monitors) vm.Load(new ColorSettings());
        ShowStatus(InfoBarSeverity.Success, "Pronto", "Todos os monitores voltaram ao padrão.");
    }

    // ---------- Copiar configuração ----------

    private void RebuildCopyTargets()
    {
        CopyTargets.Clear();
        if (SelectedMonitor is null) return;
        CopyTargets.Add(new CopyTarget("Todos os outros", null));
        foreach (var vm in Monitors.Where(m => m != SelectedMonitor))
            CopyTargets.Add(new CopyTarget(vm.DisplayName, vm));
        SelectedCopyTarget = CopyTargets[0];
    }

    [RelayCommand]
    private void CopySettings()
    {
        if (SelectedMonitor is null || SelectedCopyTarget is null) return;
        var source = SelectedMonitor.ToSettings();
        var targets = SelectedCopyTarget.Monitor is { } one
            ? new[] { one }
            : Monitors.Where(m => m != SelectedMonitor).ToArray();

        foreach (var vm in targets) vm.Load(source);
        ShowStatus(InfoBarSeverity.Success, "Configuração copiada",
            $"Ajustes da {SelectedMonitor.DisplayName} aplicados em: {string.Join(", ", targets.Select(t => t.DisplayName))}.");
    }

    // ---------- Perfis (presets) ----------

    public IEnumerable<string> GetPresetNames() => _presets.Select(p => p.Name);

    [RelayCommand]
    private void SavePreset()
    {
        var name = string.IsNullOrWhiteSpace(NewPresetName) ? SelectedPreset : NewPresetName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowStatus(InfoBarSeverity.Warning, "Dê um nome ao perfil", "Digite um nome como \"Jogo\", \"Leitura\" ou \"Noite\".");
            return;
        }

        var preset = new Preset
        {
            Name = name,
            Monitors = Monitors.Select(m => new PresetMonitor
            {
                StableId = m.StableId,
                DisplayName = m.DisplayName,
                Settings = m.ToSettings()
            }).ToList()
        };
        // Mesmo nome = substitui o perfil antigo.
        _presets.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        _presets.Add(preset);
        _presets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        _presetService.SavePresets(_presets);

        RefreshPresetNames();
        SelectedPreset = name;
        NewPresetName = "";
        ShowStatus(InfoBarSeverity.Success, "Perfil salvo", $"\"{name}\" guardou os ajustes de {preset.Monitors.Count} monitor(es).");
    }

    [RelayCommand]
    public void LoadPreset(string? name = null)
    {
        name ??= SelectedPreset;
        var preset = _presets.FirstOrDefault(p => p.Name == name);
        if (preset is null) return;

        // Procura cada monitor do perfil pelo ID estável; os que não estão conectados são listados no aviso.
        var missing = new List<string>();
        foreach (var entry in preset.Monitors)
        {
            var vm = Monitors.FirstOrDefault(m => m.StableId == entry.StableId);
            if (vm is null) missing.Add(entry.DisplayName);
            else vm.Load(entry.Settings);
        }

        if (missing.Count == 0)
            ShowStatus(InfoBarSeverity.Success, "Perfil carregado", $"\"{preset.Name}\" aplicado.");
        else
            ShowStatus(InfoBarSeverity.Warning, $"Perfil \"{preset.Name}\" aplicado parcialmente",
                $"Não conectado(s) ou em outra posição: {string.Join(", ", missing)}.");
    }

    [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedPreset is null) return;
        var name = SelectedPreset;
        _presets.RemoveAll(p => p.Name == name);
        _presetService.SavePresets(_presets);
        RefreshPresetNames();
        ShowStatus(InfoBarSeverity.Informational, "Perfil excluído", $"\"{name}\" foi removido.");
    }

    private void RefreshPresetNames()
    {
        PresetNames.Clear();
        foreach (var p in _presets) PresetNames.Add(p.Name);
        SelectedPreset = PresetNames.FirstOrDefault();
    }

    // ---------- Estado e encerramento ----------

    /// <summary>Guarda os ajustes atuais para reaplicar na próxima vez que o app abrir.</summary>
    public void SaveState()
    {
        foreach (var vm in Monitors) _state.Monitors[vm.StableId] = vm.ToSettings();
        _presetService.SaveState(_state);
    }

    /// <summary>
    /// Salva o estado e devolve TODOS os monitores à rampa original. Pode ser chamado por
    /// Closing, OnExit e ProcessExit — a flag garante que só roda uma vez.
    /// </summary>
    public void Shutdown()
    {
        if (_isShutDown) return;
        _isShutDown = true;
        try { SaveState(); } catch { /* não impede a restauração */ }
        _gammaService.RestoreAll(Monitors.Select(m => m.Info).ToList());
    }

    private void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusSeverity = severity;
        StatusTitle = title;
        StatusMessage = message;
        IsStatusOpen = true;
    }
}
