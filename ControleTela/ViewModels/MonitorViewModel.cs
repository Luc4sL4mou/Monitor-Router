using CommunityToolkit.Mvvm.ComponentModel;
using ControleTela.Models;

namespace ControleTela.ViewModels;

/// <summary>
/// Um monitor na tela: as informações dele + os valores dos sliders.
/// [ObservableProperty] gera a propriedade pública (ex.: Brightness) e avisa a interface
/// sempre que o valor muda. Os métodos OnXxxChanged são chamados automaticamente.
/// </summary>
public partial class MonitorViewModel : ObservableObject
{
    // Enquanto carregamos vários valores de uma vez, não queremos aplicar a rampa 7 vezes.
    private bool _suppressChanges;

    public MonitorViewModel(MonitorInfo info, ColorSettings settings)
    {
        Info = info;
        _suppressChanges = true;
        SetValues(settings);
        _suppressChanges = false;
    }

    /// <summary>Disparado quando qualquer slider muda (o MainViewModel aplica a rampa).</summary>
    public event EventHandler? SettingsChanged;

    public MonitorInfo Info { get; private set; }
    public string StableId => Info.StableId;
    public int Number => Info.Number;
    public string DisplayName => Info.DisplayName;
    public string Details =>
        $"{Info.Width} × {Info.Height}  ·  {Info.DeviceString}{(Info.IsPrimary ? "  ·  Vídeo principal" : "")}";

    [ObservableProperty] private bool isHdr;

    [ObservableProperty] private double brightness;
    [ObservableProperty] private double contrast;
    [ObservableProperty] private double gamma;
    [ObservableProperty] private double red;
    [ObservableProperty] private double green;
    [ObservableProperty] private double blue;
    [ObservableProperty] private double temperature;

    partial void OnBrightnessChanged(double value) => RaiseSettingsChanged();
    partial void OnContrastChanged(double value) => RaiseSettingsChanged();
    partial void OnGammaChanged(double value) => RaiseSettingsChanged();
    partial void OnRedChanged(double value) => RaiseSettingsChanged();
    partial void OnGreenChanged(double value) => RaiseSettingsChanged();
    partial void OnBlueChanged(double value) => RaiseSettingsChanged();
    partial void OnTemperatureChanged(double value) => RaiseSettingsChanged();

    /// <summary>Atualiza os dados do Windows (ex.: resolução mudou) mantendo os ajustes.</summary>
    public void UpdateInfo(MonitorInfo info)
    {
        Info = info;
        OnPropertyChanged(nameof(Info));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Details));
    }

    public ColorSettings ToSettings() => new()
    {
        Brightness = Brightness,
        Contrast = Contrast,
        Gamma = Gamma,
        Red = Red,
        Green = Green,
        Blue = Blue,
        Temperature = (int)Math.Round(Temperature)
    };

    /// <summary>Carrega todos os valores de uma vez e aplica a rampa uma única vez no final.</summary>
    public void Load(ColorSettings settings)
    {
        _suppressChanges = true;
        try { SetValues(settings); }
        finally { _suppressChanges = false; }
        RaiseSettingsChanged();
    }

    private void SetValues(ColorSettings s)
    {
        var safe = s.Clone();
        safe.Clamp();
        Brightness = safe.Brightness;
        Contrast = safe.Contrast;
        Gamma = safe.Gamma;
        Red = safe.Red;
        Green = safe.Green;
        Blue = safe.Blue;
        Temperature = safe.Temperature;
    }

    private void RaiseSettingsChanged()
    {
        if (!_suppressChanges) SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
