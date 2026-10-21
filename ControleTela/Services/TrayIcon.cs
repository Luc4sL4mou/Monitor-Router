using System.IO;
using Forms = System.Windows.Forms;

namespace ControleTela.Services;

/// <summary>
/// Ícone perto do relógio. O WPF não tem um, então usamos o NotifyIcon do WinForms
/// (por isso UseWindowsForms=true no .csproj). O menu é refeito a cada abertura para
/// sempre mostrar a lista atual de perfis.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;

    public TrayIcon(Action open, Action resetAll, Func<IEnumerable<string>> presetNames,
                    Action<string> loadPreset, Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Opening += (_, _) =>
        {
            menu.Items.Clear();
            menu.Items.Add("Abrir Monitor Router", null, (_, _) => open());

            var presets = new Forms.ToolStripMenuItem("Perfis");
            foreach (var name in presetNames())
                presets.DropDownItems.Add(name, null, (_, _) => loadPreset(name));
            presets.Enabled = presets.DropDownItems.Count > 0;
            menu.Items.Add(presets);

            menu.Items.Add("Redefinir todos", null, (_, _) => resetAll());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Sair (restaura as cores)", null, (_, _) => exit());
        };

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Monitor Router",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) open(); };
    }

    // Tenta o .ico na pasta do app no tamanho pequeno (nítido); senão usa o ícone do .exe.
    private static System.Drawing.Icon LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        try { return new System.Drawing.Icon(path, Forms.SystemInformation.SmallIconSize); }
        catch { return System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!; }
    }

    public void ShowTip(string title, string text) =>
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false; // sem isso o ícone "fantasma" fica na bandeja até passar o mouse
        _icon.Dispose();
    }
}
