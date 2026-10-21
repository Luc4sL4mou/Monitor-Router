namespace ControleTela.Models;

/// <summary>
/// Os ajustes de UM monitor. Os valores padrão (1.0, 0, 6500K...) geram uma rampa
/// linear = "sem alteração". Os limites abaixo são as "faixas seguras": o Windows recusa
/// curvas muito extremas (ex.: tela toda preta), então não deixamos o usuário chegar lá.
/// São 'double' para o XAML poder usá-los direto em Slider.Minimum/Maximum.
/// </summary>
public sealed class ColorSettings
{
    public const double BrightnessMin = -0.3, BrightnessMax = 0.3;
    public const double ContrastMin = 0.5, ContrastMax = 1.5;
    public const double GammaMin = 0.5, GammaMax = 2.0;
    // Ganho de canal: 0 a 1 na fórmula, mas abaixo de ~0.3 o Windows costuma recusar.
    public const double GainMin = 0.3, GainMax = 1.0;
    public const double TemperatureMin = 2700, TemperatureMax = 6500;

    public double Brightness { get; set; } = 0;
    public double Contrast { get; set; } = 1;
    public double Gamma { get; set; } = 1;
    public double Red { get; set; } = 1;
    public double Green { get; set; } = 1;
    public double Blue { get; set; } = 1;

    /// <summary>6500K = neutro (não altera nada). Menor = mais quente/alaranjado.</summary>
    public int Temperature { get; set; } = (int)TemperatureMax;

    public ColorSettings Clone() => (ColorSettings)MemberwiseClone();

    /// <summary>True quando nada foi alterado (a rampa resultante seria linear).</summary>
    public bool IsDefault =>
        Math.Abs(Brightness) < 0.001 && Math.Abs(Contrast - 1) < 0.001 && Math.Abs(Gamma - 1) < 0.001 &&
        Red > 0.999 && Green > 0.999 && Blue > 0.999 && Temperature >= TemperatureMax;

    /// <summary>Força todos os valores para dentro das faixas seguras.</summary>
    public void Clamp()
    {
        Brightness = Math.Clamp(Brightness, BrightnessMin, BrightnessMax);
        Contrast = Math.Clamp(Contrast, ContrastMin, ContrastMax);
        Gamma = Math.Clamp(Gamma, GammaMin, GammaMax);
        Red = Math.Clamp(Red, GainMin, GainMax);
        Green = Math.Clamp(Green, GainMin, GainMax);
        Blue = Math.Clamp(Blue, GainMin, GainMax);
        Temperature = (int)Math.Clamp(Temperature, TemperatureMin, TemperatureMax);
    }
}
