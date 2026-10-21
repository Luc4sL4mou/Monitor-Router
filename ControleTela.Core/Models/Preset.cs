namespace ControleTela.Models;

/// <summary>Um perfil nomeado ("Jogo", "Leitura"...) com os ajustes de vários monitores.</summary>
public sealed class Preset
{
    public string Name { get; set; } = "";
    public List<PresetMonitor> Monitors { get; set; } = new();
}

/// <summary>
/// Ajustes de um monitor dentro de um perfil. Guardamos também o nome legível para
/// poder avisar "Tela 3 - LG TV não está conectada" quando o monitor sumir.
/// </summary>
public sealed class PresetMonitor
{
    public string StableId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ColorSettings Settings { get; set; } = new();
}

/// <summary>Estado salvo entre execuções: os últimos ajustes de cada monitor (por StableId).</summary>
public sealed class AppState
{
    public Dictionary<string, ColorSettings> Monitors { get; set; } = new();
}
