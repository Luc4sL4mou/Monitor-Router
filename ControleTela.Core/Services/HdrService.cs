using ControleTela.Native;
using System.Runtime.InteropServices;
using static ControleTela.Native.DisplayConfigNative;

namespace ControleTela.Services;

/// <summary>
/// Descobre quais monitores estão com HDR ligado. Com HDR, o Windows passa a controlar a cor
/// por outro caminho e a Gamma Ramp normalmente é ignorada (ou tem efeito estranho).
/// </summary>
public static class HdrService
{
    /// <summary>Devolve os DeviceNames (ex.: \\.\DISPLAY1) com HDR ativo.</summary>
    public static HashSet<string> GetHdrDisplays()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount) != 0)
                return result;

            var paths = new PATH_INFO[pathCount];
            var modes = new MODE_INFO[modeCount];
            if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
                return result;

            foreach (var path in paths.Take((int)pathCount))
            {
                // 1) Qual \\.\DISPLAYn corresponde a este caminho?
                var source = new SOURCE_DEVICE_NAME
                {
                    header = Header(DEVICE_INFO_GET_SOURCE_NAME, Marshal.SizeOf<SOURCE_DEVICE_NAME>(),
                                    path.sourceInfo.adapterId, path.sourceInfo.id),
                    viewGdiDeviceName = ""
                };
                if (DisplayConfigGetDeviceInfo(ref source) != 0) continue;

                // 2) O monitor (target) está em HDR?
                if (IsHdr(path.targetInfo))
                    result.Add(source.viewGdiDeviceName);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // Windows antigo sem a API: considera que não há HDR.
        }
        return result;
    }

    private static bool IsHdr(PATH_TARGET_INFO target)
    {
        // Tenta a versão nova (24H2), que distingue HDR de "gerenciamento automático de cor".
        var info2 = new ADVANCED_COLOR_INFO_2
        {
            header = Header(DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2, Marshal.SizeOf<ADVANCED_COLOR_INFO_2>(),
                            target.adapterId, target.id)
        };
        if (DisplayConfigGetDeviceInfo(ref info2) == 0)
            return info2.activeColorMode == ADVANCED_COLOR_MODE_HDR;

        // Versões anteriores: bit 1 = cor avançada ligada.
        var info = new ADVANCED_COLOR_INFO
        {
            header = Header(DEVICE_INFO_GET_ADVANCED_COLOR_INFO, Marshal.SizeOf<ADVANCED_COLOR_INFO>(),
                            target.adapterId, target.id)
        };
        return DisplayConfigGetDeviceInfo(ref info) == 0 && (info.value & 0x2) != 0;
    }

    private static DEVICE_INFO_HEADER Header(uint type, int size, LUID adapter, uint id) =>
        new() { type = type, size = (uint)size, adapterId = adapter, id = id };
}
