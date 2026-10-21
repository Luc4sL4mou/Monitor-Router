<p align="center">
  <img src="docs/logo.png" alt="Monitor Router" width="220">
</p>

# Monitor Router

Aplicativo para Windows 11 (C# / .NET 10 / WPF) que ajusta **brilho, contraste, gama, cor (R/G/B) e temperatura de cor de cada monitor ou TV de forma independente**, com visual Fluent (WPF-UI) no estilo das Configurações do Windows 11.

## Como funciona

O app usa a **Gamma Ramp** da placa de vídeo: uma tabela de 256 entradas por canal (R, G, B), com valores de 0 a 65535, que o driver aplica ao sinal enviado ao monitor. Como o ajuste é feito no sinal da GPU, funciona em qualquer monitor ou TV ligado por HDMI/DisplayPort.

Para cada monitor, a tabela é calculada com:

```
v = i/255
v = (v - 0.5) * contraste + 0.5 + brilho
v = clamp(v, 0, 1)
v = v ^ (1 / gama)
canal = clamp(v * ganho, 0, 1) * 65535
```

A rampa é aplicada em **um** monitor por vez: `CreateDC("DISPLAY", deviceName)` → `SetDeviceGammaRamp` → `DeleteDC`.

## Recursos

- Mapa dos monitores na posição real, clicável, e botão **Identificar**
- Sliders em tempo real: brilho, contraste, gama, vermelho, verde, azul e temperatura
- **Redefinir** (um monitor) e **Redefinir todos**
- **Copiar configuração** para outro monitor ou para todos os outros
- **Perfis** nomeados (ex.: "Jogo", "Leitura", "Noite"), salvos em `%AppData%\ControleTela`
- Reaplica os ajustes quando os monitores mudam (`DisplaySettingsChanged`)
- Ícone na bandeja e opção de iniciar com o Windows (desligada por padrão)
- Restaura as cores originais ao fechar
- Avisos de HDR ativo, Luz noturna ligada e ajustes recusados pelo Windows

## Estrutura

| Projeto | Conteúdo |
|---|---|
| `ControleTela.Core` | Models, Services (enumeração de monitores, Gamma Ramp, perfis, HDR) e chamadas nativas (P/Invoke) |
| `ControleTela.Console` | Programa de teste em linha de comando |
| `ControleTela` | Interface WPF (Views, ViewModels com CommunityToolkit.Mvvm, WPF-UI) |

## Compilar e executar

Requer o SDK do .NET 10 e Windows 11.

```powershell
dotnet run --project ControleTela            # interface gráfica
dotnet run --project ControleTela.Console    # teste via console
```

Gerar um executável único e autocontido (não exige o .NET instalado):

```powershell
dotnet publish ControleTela -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish
```

O resultado é `publish\MonitorRouter.exe`.

## Se algum monitor não responder

- **"Não foi possível ajustar":** o Windows recusa curvas muito extremas. Use valores menos extremos.
- **Nada muda:** desligue o HDR, a Luz noturna, o f.lux e o ajuste de cor do painel da NVIDIA/AMD, que usam a mesma tabela.
- **Adaptadores/docks (DisplayLink, alguns USB-C):** o driver costuma ignorar a Gamma Ramp.
- **Tela estranha:** feche o app ou reinicie o driver de vídeo com **Win+Ctrl+Shift+B**.
