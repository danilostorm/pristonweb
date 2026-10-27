#requires -RunAsAdministrator
<#
.SYNOPSIS
  Adiciona apresentacao fullscreen responsiva ao cliente Priston Tale Web.
.DESCRIPTION
  Nao altera WebAssembly, saves, rede ou renderizador do jogo; preserva
  resolucao logica 1024x768 e escala o canvas proporcionalmente.
  ?debug=1 mostra barra e log originais para suporte.
#>
[CmdletBinding()]
param([string]$WebsitePath = 'C:\inetpub\priston')
$ErrorActionPreference = 'Stop'
$site = (Resolve-Path -LiteralPath $WebsitePath).Path
$indexPath = Join-Path $site 'index.html'
$cssTarget = Join-Path $site 'stormpt-ui.css'
$backupDir = 'C:\PristonDeploy\backups'
New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if (-not (Test-Path $indexPath)) { throw "index.html nao encontrado: $indexPath" }

$cssSource = Join-Path (Split-Path -Parent $PSScriptRoot) 'overrides\stormpt-ui.css'
if (Test-Path -LiteralPath $cssSource) {
    Copy-Item -LiteralPath $cssSource -Destination $cssTarget -Force
} else {
    $uri = 'https://raw.githubusercontent.com/danilostorm/pristonweb/main/overrides/stormpt-ui.css'
    & curl.exe -fLsS --connect-timeout 10 --max-time 50 --output $cssTarget $uri
    if ($LASTEXITCODE -ne 0) { throw "Falha no download de $uri" }
}
$cssContent = [IO.File]::ReadAllText($cssTarget)
if ($cssContent -notmatch 'pt-load\.big' -or $cssContent -notmatch '100dvh') {
    throw 'CSS de fullscreen nao passou na validacao'
}

$html = [IO.File]::ReadAllText($indexPath)
if ($html -notmatch '<canvas\s' -or $html -notmatch 'id=canvas') {
    throw 'HTML inesperado: cliente sem canvas identificado'
}
$injection = '<link rel="stylesheet" href="stormpt-ui.css"><script>try{if(new URLSearchParams(location.search).get("debug")==="1")document.documentElement.classList.add("stormpt-debug")}catch(e){}</script>'
if ($html -notmatch 'stormpt-ui\.css') {
    if (-not $html.Contains('</head>')) { throw 'HTML sem </head>' }
    Copy-Item -LiteralPath $indexPath -Destination (Join-Path $backupDir "index-$stamp.html")
    $html = $html.Replace('</head>', $injection + '</head>')
    [IO.File]::WriteAllText($indexPath, $html, (New-Object Text.UTF8Encoding($false)))
    Write-Output 'FULLSCREEN_HTML_UPDATED'
} else {
    Write-Output 'FULLSCREEN_HTML_ALREADY_PATCHED'
}
Write-Output 'FULLSCREEN_CSS_INSTALLED'
Write-Output 'DEBUG_VIEW=https://stormpt.hoststorm.cloud/?debug=1'
