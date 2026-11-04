@echo off
setlocal
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo [1/3] Stopping Lenovo services...
sc stop SLBSafe >nul 2>&1
sc delete SLBSafe >nul 2>&1
sc stop LenovoPcManagerService >nul 2>&1
sc stop SLBService >nul 2>&1
sc stop LenovoServiceAS >nul 2>&1
sc stop LenovoVantageService >nul 2>&1
sc stop LnvVCamInstaller >nul 2>&1
sc stop SmartSense >nul 2>&1

echo [2/3] Killing Lenovo processes...
taskkill /f /im SLBrowser.exe >nul 2>&1
taskkill /f /im LenovoPcManagerService.exe >nul 2>&1
taskkill /f /im LenovoTray.exe >nul 2>&1
taskkill /f /im LAVService.exe >nul 2>&1
taskkill /f /im LenovoAppStore.exe >nul 2>&1
taskkill /f /im WSPluginHost.exe >nul 2>&1
taskkill /f /im fastpdf.exe >nul 2>&1
taskkill /f /im cfpixeleditor.exe >nul 2>&1
taskkill /f /im kvipgui.exe >nul 2>&1

echo [3/3] Deleting folders...
rd /s /q "C:\Program Files (x86)\Lenovo" 2>nul
rd /s /q "C:\Program Files\Lenovo" 2>nul
rd /s /q "D:\2022sw" 2>nul

echo.
if exist "C:\Program Files (x86)\Lenovo" (
    echo PARTIAL: some files still locked. Close all Lenovo apps and run again.
) else (
    echo DONE: Lenovo folders removed.
)
if exist "D:\2022sw" (echo NOTE: D:\2022sw still has a few files) else (echo DONE: D:\2022sw removed.)
echo.
pause
