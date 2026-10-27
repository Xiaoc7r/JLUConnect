@echo off
cd /d "%~dp0"
if exist "%~dp0install.ps1" (
 powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
) else (
 echo Please download the Windows package from GitHub Releases first.
 pause
)
