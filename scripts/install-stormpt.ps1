#requires -RunAsAdministrator
<#
.SYNOPSIS
  Instala o site e o servidor Windows do StormPT a partir do submodulo upstream.
.DESCRIPTION
  Nao abre portas no roteador, nao publica o servidor de jogo e nao apaga DataServer.
#>
[CmdletBinding()]
param(
  [string]$SourceRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'upstream'),
  [string]$WebsitePath = 'C:\inetpub\priston',
  [string]$ServerPath = 'C:\PristonServer'
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$source = (Resolve-Path -LiteralPath $SourceRoot -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath (Join-Path $source 'game.wasm'))) {
  throw "Arquivos do submodulo ausentes. Execute: git submodule update --init --recursive"
}
$gameServer = Join-Path $source '[project]\dist\server'
if (-not (Test-Path -LiteralPath (Join-Path $gameServer 'server.exe'))) {
  throw "Executavel do servidor nao encontrado: $gameServer"
}

$features = @(
  'IIS-WebServerRole','IIS-WebServer','IIS-StaticContent','IIS-DefaultDocument',
  'IIS-WebSockets','IIS-ASPNET45','IIS-NetFxExtensibility45',
  'IIS-ISAPIExtensions','IIS-ISAPIFilter','IIS-ManagementConsole'
)
$missing = @()
foreach ($f in $features) {
  $item = Get-WindowsOptionalFeature -Online -FeatureName $f
  if ($item.State -ne 'Enabled') { $missing += $f }
}
if ($missing.Count -gt 0) {
  Write-Host 'Habilitando IIS e ASP.NET 4.8...'
  $result = Enable-WindowsOptionalFeature -Online -All -FeatureName $missing -NoRestart
  if ($result.RestartNeeded) { throw 'IIS requer reinicializacao. Reinicie e execute este script novamente.' }
}
New-Item -ItemType Directory -Force -Path $WebsitePath, $ServerPath | Out-Null

$siteFiles = @(
  'index.html','game.html','game.js','game.wasm','sw.js','progress.js','config.js',
  'ws.ashx','web.config','data_startup.pack','data_manifest.txt','admin.js','server_worker.js'
)
foreach ($name in $siteFiles) {
  $from = Join-Path $source $name
  if (Test-Path -LiteralPath $from) {
    Copy-Item -LiteralPath $from -Destination (Join-Path $WebsitePath $name) -Force
  } else {
    throw "Arquivo do site ausente: $name"
  }
}
# Reaplica as correcoes locais depois de copiar os arquivos do upstream.
$overrideDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'overrides'
$serviceWorkerOverride = Join-Path $overrideDir 'sw.js'
if (Test-Path -LiteralPath $serviceWorkerOverride) {
  Copy-Item -LiteralPath $serviceWorkerOverride -Destination (Join-Path $WebsitePath 'sw.js') -Force
  Write-Host 'Service worker otimizado restaurado.'
}

Write-Host 'Copiando dados do jogo (operacao longa)...'
& robocopy.exe (Join-Path $source 'data') (Join-Path $WebsitePath 'data') /E /R:2 /W:2 /NFL /NDL /NP
if ($LASTEXITCODE -ge 8) { throw "Erro robocopy dados: $LASTEXITCODE" }

Write-Host 'Copiando executavel e dados de mundo...'
# /E copia e atualiza sem apagar arquivos existentes (especialmente DataServer).
& robocopy.exe $gameServer $ServerPath /E /R:2 /W:2 /NFL /NDL /NP
if ($LASTEXITCODE -ge 8) { throw "Erro robocopy servidor: $LASTEXITCODE" }

$appcmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'
if (-not (Test-Path -LiteralPath $appcmd)) { throw 'appcmd.exe ausente. Verifique o IIS e reinicie o Windows.' }
& $appcmd set vdir 'Default Web Site/' "/physicalPath:$WebsitePath"
if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel configurar o caminho do site IIS.' }
& $appcmd set apppool /apppool.name:DefaultAppPool /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated
if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel configurar o Application Pool.' }

if ((Get-Service W3SVC).Status -ne 'Running') { Start-Service W3SVC -ErrorAction Stop }

# Apenas a rede LAN pode acessar HTTP nesta fase; nao habilita HTTPS publico nem TCP 10007.
$lanRule = 'StormPT IIS HTTP LAN'
if (-not (Get-NetFirewallRule -DisplayName $lanRule -ErrorAction SilentlyContinue)) {
  New-NetFirewallRule -DisplayName $lanRule -Direction Inbound -Action Allow -Protocol TCP -LocalPort 80 -RemoteAddress '192.168.30.0/24' | Out-Null
}
Write-Host "Site instalado: $WebsitePath"
Write-Host "Servidor separado: $ServerPath"
Write-Host 'Teste o site em http://localhost/ depois de executar C:\PristonServer\server.exe'
Write-Host 'Configure HTTPS antes de permitir jogadores pela rede.'
