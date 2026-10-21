using System.Runtime.InteropServices;

namespace ControleTela.Native;

/// <summary>
/// API "DisplayConfig" do Windows (user32), usada só para descobrir se o HDR está ligado.
/// As structs seguem o layout de wingdi.h; os campos que não usamos existem só para ocupar
/// o espaço certo na memória.
/// </summary>
internal static class DisplayConfigNative
{
    public const uint QDC_ONLY_ACTIVE_PATHS = 0x2;
    public const uint DEVICE_INFO_GET_SOURCE_NAME = 1;
    public const uint DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
    public const uint DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 = 15; // Windows 11 24H2+
    public const uint ADVANCED_COLOR_MODE_HDR = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public uint refreshRateNumerator;
        public uint refreshRateDenominator;
        public uint scanLineOrdering;
        public int targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PATH_INFO
    {
        public PATH_SOURCE_INFO sourceInfo;
        public PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    /// <summary>64 bytes; a "union" de 48 bytes no fim é guardada como 6 ulongs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        public ulong u0, u1, u2, u3, u4, u5;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SOURCE_DEVICE_NAME
    {
        public DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
    }

    /// <summary>value: bit 0 = suporta cor avançada, bit 1 = cor avançada (HDR) ligada.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ADVANCED_COLOR_INFO
    {
        public DEVICE_INFO_HEADER header;
        public uint value;
        public uint colorEncoding;
        public uint bitsPerColorChannel;
    }

    /// <summary>Versão 24H2: activeColorMode diz exatamente se é SDR (0), WCG (1) ou HDR (2).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ADVANCED_COLOR_INFO_2
    {
        public DEVICE_INFO_HEADER header;
        public uint value;
        public uint colorEncoding;
        public uint bitsPerColorChannel;
        public uint activeColorMode;
    }

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(uint flags, ref uint numPathArrayElements, [Out] PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements, [Out] MODE_INFO[] modeInfoArray, IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref SOURCE_DEVICE_NAME requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref ADVANCED_COLOR_INFO requestPacket);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(ref ADVANCED_COLOR_INFO_2 requestPacket);
}
