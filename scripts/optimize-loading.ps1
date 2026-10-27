#requires -RunAsAdministrator
<#
.SYNOPSIS
  Otimiza downloads do cliente Priston Tale no IIS Windows.
.DESCRIPTION
  Habilita gzip de arquivos estaticos binarios, sem alterar os assets do jogo.
  Aplica o service worker corrigido da pasta overrides/ se presente.
  As regras de cache e compressao da Cloudflare sao gerenciadas separadamente.
#>
[CmdletBinding()]
param(
  [string]$WebsitePath = 'C:\inetpub\priston'
)
$ErrorActionPreference = 'Stop'
$backupDir = 'C:\PristonDeploy\backups'
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$apphost = Join-Path $env:windir 'System32\inetsrv\config\applicationHost.config'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Copy-Item -LiteralPath $apphost -Destination (Join-Path $backupDir "applicationHost-$stamp.config")

Add-Type -Path (Join-Path $env:windir 'System32\inetsrv\Microsoft.Web.Administration.dll')
$manager = New-Object Microsoft.Web.Administration.ServerManager
try {
  $section = $manager.GetApplicationHostConfiguration().GetSection('system.webServer/httpCompression')
  $types = $section.GetChildElement('staticTypes').GetCollection()
  $existing = @($types | Where-Object { $_['mimeType'] -eq 'application/octet-stream' })
  if ($existing.Count -eq 0) {
    $entry = $types.CreateElement('add')
    $entry['mimeType'] = 'application/octet-stream'
    $entry['enabled'] = $true
    # Position ahead of the catch-all "*/* disabled".
    $types.AddAt(0, $entry)
    $manager.CommitChanges()
  } elseif (-not [bool]$existing[0]['enabled']) {
    $existing[0]['enabled'] = $true
    $manager.CommitChanges()
  }
  Write-Host 'IIS binary static compression configured.'
} finally {
  $manager.Dispose()
}

$override = Join-Path (Split-Path -Parent $PSScriptRoot) 'overrides\sw.js'
$target = Join-Path $WebsitePath 'sw.js'
if (Test-Path -LiteralPath $override) {
  if (-not (Test-Path -LiteralPath $target)) { throw "Missing service worker: $target" }
  Copy-Item -LiteralPath $target -Destination (Join-Path $backupDir "sw-$stamp.js") -Force
  Copy-Item -LiteralPath $override -Destination $target -Force
  Write-Host "Patched service worker installed: $target"
} else {
  Write-Warning 'No overrides/sw.js found next to this script. Only IIS compression configured.'
}
Write-Host 'Check with: curl.exe -I -H "Accept-Encoding: gzip" http://localhost/data_startup.pack'
