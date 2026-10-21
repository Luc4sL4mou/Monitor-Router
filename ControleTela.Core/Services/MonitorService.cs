using ControleTela.Models;
using ControleTela.Native;
using System.Runtime.InteropServices;

namespace ControleTela.Services;

public sealed class MonitorService
{
    /// <summary>
    /// Lista os monitores ativos. O Windows organiza em dois níveis:
    ///   "adaptador" (saída da placa, ex.: \\.\DISPLAY1)  ->  "monitor" ligado nela.
    /// Por isso há um EnumDisplayDevices externo (adaptadores) e um interno (o monitor).
    /// </summary>
    public IReadOnlyList<MonitorInfo> GetActiveMonitors()
    {
        var result = new List<MonitorInfo>();

        for (uint i = 0; ; i++)
        {
            var adapter = NewDisplayDevice();
            if (!NativeMethods.EnumDisplayDevices(null, i, ref adapter, 0))
                break; // acabaram os adaptadores

            // Ignora saídas que existem mas não estão ligadas a um monitor ativo.
            if ((adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
                continue;

            // Nível 2: o monitor conectado nesta saída (dá o nome amigável).
            var monitor = NewDisplayDevice();
            var monitorName = NativeMethods.EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0)
                ? monitor.DeviceString
                : "";

            // Posição e tamanho atuais na área de trabalho virtual.
            var mode = new NativeMethods.DEVMODE
            {
                dmDeviceName = "",
                dmFormName = "",
                dmSize = (ushort)Marshal.SizeOf<NativeMethods.DEVMODE>()
            };
            NativeMethods.EnumDisplaySettings(adapter.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref mode);

            result.Add(new MonitorInfo
            {
                DeviceName = adapter.DeviceName,
                DeviceString = adapter.DeviceString,
                MonitorName = monitorName,
                IsPrimary = (adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0,
                X = mode.dmPositionX,
                Y = mode.dmPositionY,
                Width = (int)mode.dmPelsWidth,
                Height = (int)mode.dmPelsHeight
            });
        }

        return result.OrderBy(m => m.Number).ToList();
    }

    // O Windows exige que o campo 'cb' (tamanho da struct) venha preenchido.
    private static NativeMethods.DISPLAY_DEVICE NewDisplayDevice() => new()
    {
        cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>(),
        DeviceName = "",
        DeviceString = "",
        DeviceID = "",
        DeviceKey = ""
    };
}
