@echo off
setlocal
title StormPT - Instalar patch porta 1997
echo.
echo ==================================================
echo  STORMPT - PATCH DO CLIENTE 1997 (29/10/2026)
echo ==================================================
echo.
echo Este instalador baixa a atualizacao do seu GitHub,
echo verifica os arquivos e cria backup do Game.exe.
echo Feche o jogo antes de continuar.
echo.
set "BOOTFILE=%TEMP%\StormPT-Install-1997-%RANDOM%.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$ProgressPreference='SilentlyContinue'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/danilostorm/pristonweb/stormpt-launcher-v0.1.0/launcher/patches/Instalar-StormPT-1997.ps1' -OutFile '%BOOTFILE%' -UseBasicParsing"
if errorlevel 1 (
  echo FALHA: nao foi possivel baixar o instalador do GitHub.
  if exist "%BOOTFILE%" del "%BOOTFILE%"
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%BOOTFILE%" -GameFolder "%~dp0"
set "RESULTADO=%ERRORLEVEL%"
del "%BOOTFILE%" >nul 2>&1
if not "%RESULTADO%"=="0" (
  echo.
  echo Instalacao nao concluida. Verifique a mensagem acima.
) else (
  echo.
  echo Patch instalado. Agora teste login e entrada na cidade.
)
echo.
pause
exit /b %RESULTADO%
