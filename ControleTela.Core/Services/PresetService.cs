using ControleTela.Models;
using System.Text.Json;

namespace ControleTela.Services;

/// <summary>
/// Lê e grava os perfis (presets.json) e o último estado (state.json) em %AppData%\ControleTela.
/// System.Text.Json converte objetos C# em texto JSON e vice-versa.
/// </summary>
public sealed class PresetService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Folder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControleTela");

    private static string PresetsPath => Path.Combine(Folder, "presets.json");
    private static string StatePath => Path.Combine(Folder, "state.json");

    public List<Preset> LoadPresets() => Read<List<Preset>>(PresetsPath);
    public void SavePresets(List<Preset> presets) => Write(PresetsPath, presets);

    public AppState LoadState() => Read<AppState>(StatePath);
    public void SaveState(AppState state) => Write(StatePath, state);

    private static T Read<T>(string path) where T : new()
    {
        if (!File.Exists(path)) return new T();
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T();
        }
        catch (JsonException)
        {
            // Arquivo corrompido: guarda uma cópia em vez de sobrescrever os dados do usuário.
            File.Copy(path, path + ".corrompido", overwrite: true);
            return new T();
        }
    }

    // Grava num arquivo temporário e depois troca: se o PC desligar no meio, o JSON antigo sobrevive.
    private static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Folder);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }
}
