@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo Starting Think A102 BLE bridge...
echo Keep this window open while riding. Close it to stop.
echo.
python a1_bridge.py
echo.
echo Bridge stopped.
pause >nul
