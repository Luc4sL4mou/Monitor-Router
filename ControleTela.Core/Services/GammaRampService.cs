using ControleTela.Models;
using ControleTela.Native;

namespace ControleTela.Services;

/// <summary>Resultado de aplicar uma rampa: sucesso ou uma mensagem amigável.</summary>
public readonly record struct ApplyResult(bool Success, string? Message)
{
    public static ApplyResult Ok() => new(true, null);
    public static ApplyResult Fail(string message) => new(false, message);
}

public sealed class GammaRampService
{
    // Rampa original de cada monitor (capturada antes da 1ª alteração), para restaurar ao sair.
    private readonly Dictionary<string, NativeMethods.RAMP> _originals = new();

    /// <summary>
    /// Calcula a tabela de 256 entradas por canal. Para cada entrada i (0..255):
    ///   v = i/255
    ///   v = (v - 0.5) * contraste + 0.5 + brilho     (contraste gira em torno do cinza médio)
    ///   v = clamp(v, 0, 1)
    ///   v = v ^ (1/gama)
    ///   canal = clamp(v * ganho, 0, 1) * 65535       (ganho = ajuste de cor R/G/B)
    /// O "ganho" já inclui a temperatura de cor.
    /// </summary>
    internal static NativeMethods.RAMP BuildRamp(ColorSettings s)
    {
        var (tr, tg, tb) = TemperatureToGains(s.Temperature);
        var ramp = new NativeMethods.RAMP
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };

        for (int i = 0; i < 256; i++)
        {
            double v = i / 255.0;
            v = (v - 0.5) * s.Contrast + 0.5 + s.Brightness;
            v = Math.Clamp(v, 0, 1);
            v = Math.Pow(v, 1.0 / s.Gamma);

            ramp.Red[i] = ToUShort(v * s.Red * tr);
            ramp.Green[i] = ToUShort(v * s.Green * tg);
            ramp.Blue[i] = ToUShort(v * s.Blue * tb);
        }
        return ramp;
    }

    private static ushort ToUShort(double v) => (ushort)Math.Round(Math.Clamp(v, 0, 1) * 65535);

    /// <summary>
    /// Converte temperatura (Kelvin) em ganhos R/G/B usando a aproximação de Tanner Helland.
    /// Dividimos pelo valor de 6500K para que 6500K = (1,1,1), isto é, "sem alteração".
    /// </summary>
    public static (double R, double G, double B) TemperatureToGains(int kelvin)
    {
        var (r, g, b) = KelvinToRgb(kelvin);
        var (r0, g0, b0) = KelvinToRgb(6500);
        return (Math.Min(1, r / r0), Math.Min(1, g / g0), Math.Min(1, b / b0));
    }

    private static (double R, double G, double B) KelvinToRgb(int kelvin)
    {
        double t = kelvin / 100.0;
        double r = t <= 66 ? 255 : 329.698727446 * Math.Pow(t - 60, -0.1332047592);
        double g = t <= 66
            ? 99.4708025861 * Math.Log(t) - 161.1195681661
            : 288.1221695283 * Math.Pow(t - 60, -0.0755148492);
        double b = t >= 66 ? 255 : t <= 19 ? 0 : 138.5177312231 * Math.Log(t - 10) - 305.0447927307;
        return (Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    /// <summary>
    /// Aplica os ajustes em UM monitor. Passos: CreateDC (abre o monitor específico),
    /// SetDeviceGammaRamp (grava a tabela), DeleteDC (libera). Não usamos GetDC(0)
    /// porque ele só enxerga o monitor principal.
    /// </summary>
    public ApplyResult Apply(MonitorInfo monitor, ColorSettings settings)
    {
        var safe = settings.Clone();
        safe.Clamp();
        return WithDc(monitor.DeviceName, hdc =>
        {
            CaptureOriginal(monitor.DeviceName, hdc);
            var ramp = BuildRamp(safe);
            return NativeMethods.SetDeviceGammaRamp(hdc, ref ramp)
                ? ApplyResult.Ok()
                : ApplyResult.Fail("O Windows ou o driver recusou este ajuste. Tente valores menos extremos " +
                                   "ou verifique se o HDR, a Luz noturna ou outro programa de cor estão ativos.");
        });
    }

    /// <summary>Devolve ao monitor a rampa que ele tinha antes de o app mexer.</summary>
    public ApplyResult Restore(MonitorInfo monitor)
    {
        if (!_originals.TryGetValue(monitor.DeviceName, out var original))
            return ApplyResult.Ok(); // nunca mexemos neste monitor
        return WithDc(monitor.DeviceName, hdc =>
        {
            var ramp = original;
            return NativeMethods.SetDeviceGammaRamp(hdc, ref ramp)
                ? ApplyResult.Ok()
                : ApplyResult.Fail("Não foi possível restaurar a rampa original.");
        });
    }

    public void RestoreAll(IEnumerable<MonitorInfo> monitors)
    {
        foreach (var m in monitors) Restore(m);
    }

    // Guarda a rampa original só na primeira vez que vemos este monitor.
    private void CaptureOriginal(string deviceName, IntPtr hdc)
    {
        if (_originals.ContainsKey(deviceName)) return;
        var ramp = new NativeMethods.RAMP
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };
        if (NativeMethods.GetDeviceGammaRamp(hdc, ref ramp))
            _originals[deviceName] = ramp;
    }

    // Abre e fecha o DC garantidamente (try/finally), mesmo se algo falhar no meio.
    private static ApplyResult WithDc(string deviceName, Func<IntPtr, ApplyResult> action)
    {
        IntPtr hdc = NativeMethods.CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            return ApplyResult.Fail($"Não foi possível acessar o monitor {deviceName} (desconectado?).");
        try { return action(hdc); }
        finally { NativeMethods.DeleteDC(hdc); }
    }
}
