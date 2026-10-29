param([string]$GameFolder = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$expectedZip = 'B37ADA8BB0A88288C5634E685E2287FEAC0FF7CE9BA06C07AB19218DFA14E309'
$expectedGame = 'F865F27126B0B6C56AE957832058F276D897D7739E915070EE6C59B84C47F69D'
$baseUrl = 'https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/patches/'
$filename = 'StormPT-Cliente-Porta1997-20261029.zip'
$parts = @(($filename + '.part00'), ($filename + '.part01'), ($filename + '.part02'))
if (!(Test-Path (Join-Path $GameFolder 'Game.exe'))) {
  $GameFolder = Read-Host 'Informe o caminho completo da pasta que contem Game.exe'
}
if (!(Test-Path (Join-Path $GameFolder 'Game.exe'))) { throw 'Game.exe nao encontrado: nenhuma alteracao foi realizada.' }
if (Get-Process -Name Game,Game33 -ErrorAction SilentlyContinue) {
  throw 'Feche Game.exe/Game33.exe antes de instalar.'
}
$stage = Join-Path $env:TEMP ('StormPT-Patch-' + [Guid]::NewGuid().ToString('N'))
New-Item -Path $stage -ItemType Directory -Force | Out-Null
$backup = Join-Path $GameFolder ('Backup-StormPT-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$didChange = $false
try {
  $zip = Join-Path $stage $filename
  $write = [IO.File]::Create($zip)
  try {
    foreach ($name in $parts) {
      $local = Join-Path $PSScriptRoot $name
      if (!(Test-Path $local)) {
        $local = Join-Path $stage $name
        Write-Host ('Baixando ' + $name)
        Invoke-WebRequest -Uri ($baseUrl + $name) -OutFile $local -UseBasicParsing
      }
      $read = [IO.File]::OpenRead($local)
      try { $read.CopyTo($write) } finally { $read.Dispose() }
    }
  } finally { $write.Dispose() }
  if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $expectedZip) {throw 'Pacote corrompido: SHA256 nao confere.'}
  $extracted = Join-Path $stage 'extracted'
  Expand-Archive -LiteralPath $zip -DestinationPath $extracted -Force
  $newGame = Join-Path $extracted 'Game.exe'
  if (!(Test-Path $newGame) -or (Get-FileHash $newGame -Algorithm SHA256).Hash -ne $expectedGame) {
    throw 'Game.exe atualizado nao passou na validacao SHA256.'
  }
  New-Item -ItemType Directory -Path $backup -Force | Out-Null
  $targetGame=Join-Path $GameFolder 'Game.exe'
  Copy-Item -LiteralPath $targetGame -Destination (Join-Path $backup 'Game.exe') -ErrorAction Stop
  $ini=Join-Path $GameFolder 'Settings.ini'
  if(Test-Path $ini){Copy-Item -LiteralPath $ini -Destination (Join-Path $backup 'Settings.ini') -ErrorAction Stop}
  $didChange = $true
  Copy-Item -LiteralPath $newGame -Destination $targetGame -Force -ErrorAction Stop
  if ((Get-FileHash $targetGame -Algorithm SHA256).Hash -ne $expectedGame){throw 'Falha ao verificar Game.exe instalado.'}
  if(Test-Path $ini){
    $enc=[Text.Encoding]::GetEncoding(1252)
    $content=$enc.GetString([IO.File]::ReadAllBytes($ini))
    $content=[regex]::Replace($content,'(?m)^PORT\s*=\s*\d+\s*$','PORT=1997')
    $content=[regex]::Replace($content,'(?m)^IP\s*=\s*\d{1,3}(?:\.\d{1,3}){3}\s*$','IP=163.176.128.81')
    [IO.File]::WriteAllBytes($ini,$enc.GetBytes($content))
  }
  $newLauncher = Join-Path $extracted 'StormPTLauncher.exe'
  $targetLauncher = Join-Path $GameFolder 'StormPTLauncher.exe'
  if((Test-Path $newLauncher) -and !(Test-Path $targetLauncher)) {
    Copy-Item -LiteralPath $newLauncher -Destination $targetLauncher
  }
  Write-Host 'STORMPT: PATCH APLICADO E VERIFICADO!' -ForegroundColor Green
  Write-Host 'IP publico: 163.176.128.81 | Porta unica: 1997'
  Write-Host ('Backup: '+$backup)
  Write-Host 'Abra o Game.exe, entre na cidade, saia e confirme se o personagem continua salvo.'
} catch {
  if($didChange -and (Test-Path (Join-Path $backup 'Game.exe'))) {
    Copy-Item -LiteralPath (Join-Path $backup 'Game.exe') -Destination (Join-Path $GameFolder 'Game.exe') -Force
    if(Test-Path (Join-Path $backup 'Settings.ini')) {
      Copy-Item -LiteralPath (Join-Path $backup 'Settings.ini') -Destination (Join-Path $GameFolder 'Settings.ini') -Force
    }
    Write-Warning 'Problema na instalacao; versao anterior restaurada.'
  }
  throw
} finally { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
