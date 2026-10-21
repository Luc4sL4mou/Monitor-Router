using ControleTela.Models;
using ControleTela.Services;
using System.Globalization;

var monitorService = new MonitorService();
var gammaService = new GammaRampService();

var monitors = monitorService.GetActiveMonitors();
if (monitors.Count == 0)
{
    Console.WriteLine("Nenhum monitor ativo encontrado.");
    return;
}

var hdrDisplays = HdrService.GetHdrDisplays();
Console.WriteLine($"Luz noturna: {(NightLightService.IsEnabled() ? "LIGADA (pode conflitar)" : "desligada")}");
Console.WriteLine("Monitores ativos:");
for (int i = 0; i < monitors.Count; i++)
{
    var m = monitors[i];
    var hdr = hdrDisplays.Contains(m.DeviceName) ? " [HDR]" : "";
    Console.WriteLine($"  [{i}] {m.DeviceName}  {m.DisplayName}{(m.IsPrimary ? " (principal)" : "")}{hdr}");
    Console.WriteLine($"      placa: {m.DeviceString} | {m.Width}x{m.Height} em ({m.X},{m.Y}) | id: {m.StableId}");
}

Console.Write("\nEscolha o monitor para testar (número entre colchetes): ");
if (!int.TryParse(Console.ReadLine(), out int index) || index < 0 || index >= monitors.Count)
{
    Console.WriteLine("Número inválido.");
    return;
}
var target = monitors[index];
var settings = new ColorSettings();

// Segurança: restaura a rampa original em qualquer tipo de saída.
void RestoreEverything() => gammaService.RestoreAll(monitors);
AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreEverything();
Console.CancelKeyPress += (_, _) => RestoreEverything();

Console.WriteLine($"""

    Ajustando: {target.DisplayName}. Comandos (aplicados na hora):
      b <{ColorSettings.BrightnessMin} a {ColorSettings.BrightnessMax}>   brilho      (ex.: b 0.1)
      c <{ColorSettings.ContrastMin} a {ColorSettings.ContrastMax}>      contraste   (ex.: c 1.2)
      g <{ColorSettings.GammaMin} a {ColorSettings.GammaMax}>      gama        (ex.: g 1.3)
      r|v|a <{ColorSettings.GainMin} a {ColorSettings.GainMax}>    ganho de vermelho / verde / azul
      t <{ColorSettings.TemperatureMin} a {ColorSettings.TemperatureMax}>   temperatura em Kelvin (ex.: t 4000)
      reset   volta ao padrão      q   sai (restaurando tudo)
    """);

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null) break;

    var parts = line.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) continue;
    if (parts[0] is "q" or "sair") break;

    if (parts[0] == "reset")
    {
        settings = new ColorSettings();
    }
    else
    {
        // Aceita "0.1" e "0,1" (vírgula decimal do português).
        if (parts.Length < 2 ||
            !double.TryParse(parts[1].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            Console.WriteLine("Comando não reconhecido. Exemplo: b 0.1");
            continue;
        }
        switch (parts[0])
        {
            case "b": settings.Brightness = value; break;
            case "c": settings.Contrast = value; break;
            case "g": settings.Gamma = value; break;
            case "r": settings.Red = value; break;
            case "v": settings.Green = value; break;
            case "a": settings.Blue = value; break;
            case "t": settings.Temperature = (int)value; break;
            default: Console.WriteLine("Comando não reconhecido."); continue;
        }
    }

    settings.Clamp(); // mantém dentro das faixas seguras
    var result = gammaService.Apply(target, settings);
    Console.WriteLine(result.Success
        ? $"OK  brilho={settings.Brightness:0.##} contraste={settings.Contrast:0.##} gama={settings.Gamma:0.##} " +
          $"R={settings.Red:0.##} G={settings.Green:0.##} B={settings.Blue:0.##} {settings.Temperature}K"
        : $"FALHA: {result.Message}");
}

RestoreEverything();
Console.WriteLine("Rampa original restaurada. Até mais!");
