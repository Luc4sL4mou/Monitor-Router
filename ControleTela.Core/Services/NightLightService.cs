using Microsoft.Win32;

namespace ControleTela.Services;

/// <summary>
/// Detecta se a "Luz noturna" do Windows está ligada. Não existe API oficial: o estado fica
/// num blob binário do registro, e o byte 18 vale 0x15 quando está ligada. É uma detecção
/// "melhor esforço" — se o formato mudar numa versão futura, simplesmente devolve false.
/// </summary>
public static class NightLightService
{
    private const string KeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current\" +
        @"default$windows.data.bluelightreduction.bluelightreductionstate\" +
        @"windows.data.bluelightreduction.bluelightreductionstate";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue("Data") is byte[] data && data.Length > 18 && data[18] == 0x15;
        }
        catch
        {
            return false;
        }
    }
}
