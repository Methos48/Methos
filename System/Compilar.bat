@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Compilar.ps1"
set "resultado=%errorlevel%"
echo.
pause
exit /b %resultado%
