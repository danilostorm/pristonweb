@echo off
setlocal
echo STORMPT - Atualizador de teste porta 1997
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-StormPT-1997.ps1" -GameFolder "%~dp0"
echo.
pause
