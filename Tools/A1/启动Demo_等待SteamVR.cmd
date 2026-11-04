@echo off
chcp 65001 >nul
setlocal
rem 改这里：新构建的 exe 路径
set "APP=D:\freebike\Releases\Windows\demo_2026-10-27\freebike.exe"

echo 等待 SteamVR (vrserver.exe) 启动...
set /a n=0
:wait
tasklist /FI "IMAGENAME eq vrserver.exe" 2>nul | find /I "vrserver.exe" >nul
if not errorlevel 1 goto ready
set /a n+=1
if %n% geq 90 goto timeout
timeout /t 1 >nul
goto wait

:timeout
echo 90 秒内没检测到 SteamVR，请先手动启动 SteamVR 并确认头显已连接。
pause
exit /b 1

:ready
echo SteamVR 已运行，再等 3 秒让 OpenXR 就绪...
timeout /t 3 >nul
start "" "%APP%"
exit /b 0
