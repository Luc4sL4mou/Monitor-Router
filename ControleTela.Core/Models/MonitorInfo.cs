namespace ControleTela.Models;

/// <summary>
/// Dados de um monitor ativo. É só informação (sem lógica): o MonitorService preenche.
/// </summary>
public sealed class MonitorInfo
{
    /// <summary>Nome do dispositivo no Windows, ex.: \\.\DISPLAY1. É o que o CreateDC precisa.</summary>
    public required string DeviceName { get; init; }

    /// <summary>Nome da placa de vídeo que alimenta esta saída, ex.: "NVIDIA GeForce RTX 3060".</summary>
    public required string DeviceString { get; init; }

    /// <summary>Nome do monitor em si, ex.: "Generic PnP Monitor" ou "LG TV". Pode vir vazio.</summary>
    public string MonitorName { get; init; } = "";

    public bool IsPrimary { get; init; }

    /// <summary>Posição (canto superior esquerdo) na área de trabalho virtual, em pixels.</summary>
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    /// <summary>Número mostrado pelo Windows em "Tela" (o final de \\.\DISPLAY2 é 2).</summary>
    public int Number
    {
        get
        {
            var digits = new string(DeviceName.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : 0;
        }
    }

    /// <summary>
    /// ID estável usado para salvar presets: nome do dispositivo + posição.
    /// Se você trocar o monitor de lugar, o ID muda e o preset não se aplica ao monitor errado.
    /// </summary>
    public string StableId => $"{DeviceName}|{X},{Y}";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(MonitorName) ? $"Tela {Number}" : $"Tela {Number} - {MonitorName}";
}
