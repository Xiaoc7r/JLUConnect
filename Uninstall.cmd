@echo off
cd /d "%~dp0"
if exist "%~dp0uninstall.ps1" (
 powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1"
) else (
 echo Please use the Windows release package.
 pause
)
