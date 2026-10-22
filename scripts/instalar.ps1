<#
  Publica o Monitor Router, instala em %LocalAppData%\Programs\Monitor Router
  e cria (ou recria) o atalho na Área de Trabalho. Não precisa de administrador.

  Uso (na raiz do repositório):  .\scripts\instalar.ps1
#>
$ErrorActionPreference = 'Stop'

$root    = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root 'publish'
$install = Join-Path $env:LOCALAPPDATA 'Programs\Monitor Router'
$exeName = 'MonitorRouter.exe'

# O .exe fica em uso enquanto o app roda: fecha pela janela (isso restaura as cores) antes de trocar o arquivo.
Get-Process MonitorRouter -ErrorAction SilentlyContinue | ForEach-Object {
    $_.CloseMainWindow() | Out-Null
    if (-not $_.WaitForExit(8000)) { $_.Kill() }
}

Write-Host 'Publicando...'
dotnet publish (Join-Path $root 'ControleTela') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou.' }

New-Item -ItemType Directory -Force $install | Out-Null
Copy-Item (Join-Path $publish $exeName) $install -Force
$exe = Join-Path $install $exeName

# O ícone vem embutido no próprio .exe (índice 0), então o atalho não depende de nenhum outro arquivo.
$lnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Monitor Router.lnk'
$s = (New-Object -ComObject WScript.Shell).CreateShortcut($lnk)
$s.TargetPath       = $exe
$s.WorkingDirectory = $install
$s.IconLocation     = "$exe,0"
$s.Description      = 'Ajusta cor, brilho, contraste e gama de cada monitor'
$s.Save()

Write-Host "Instalado em: $exe"
Write-Host "Atalho:       $lnk"
