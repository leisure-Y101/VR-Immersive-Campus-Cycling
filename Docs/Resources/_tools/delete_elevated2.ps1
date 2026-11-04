# Elevated pass 2: stop/disable Lenovo services, kill processes, delete remaining folders.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'elevated_cleanup2.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

$svcs = @('LenovoPcManagerService','LenovoProcessManagement','LnvVCamInstaller','LnvSvcFdn','SLBService',
          'LenovoServiceAS','LenovoVantageService','LenovoSLBrowserElevationService','SmartSense','HRWSCCtrl')
foreach ($s in $svcs) {
    $svc = Get-Service -Name $s
    if (-not $svc) { continue }
    Log "service $s : $($svc.Status) -> stop + disable"
    Stop-Service -Name $s -Force -ErrorAction SilentlyContinue
    Set-Service -Name $s -StartupType Disabled -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 1

foreach ($p in (Get-Process | Where-Object { $_.Path -like 'C:\Program Files\Lenovo\*' -or $_.Path -like 'C:\Program Files (x86)\Lenovo\*' })) {
    Log "kill $($p.ProcessName) $($p.Id)"
    try { $p.Kill() } catch {}
}
Start-Sleep -Seconds 2

foreach ($sub in @('C:\Program Files (x86)\Lenovo\LeAppStore','C:\Program Files (x86)\Lenovo\PCManager','C:\Program Files (x86)\Lenovo\SLBrowser','C:\Program Files (x86)\Lenovo','C:\Program Files\Lenovo','D:\2022sw')) {
    if (-not (Test-Path -LiteralPath $sub)) { Log "gone: $sub"; continue }
    Remove-Item -LiteralPath $sub -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $sub) {
        $sz = (Get-ChildItem -LiteralPath $sub -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("LEFTOVER {0} ({1:N2} GB)" -f $sub, ($sz/1GB))
    } else { Log "removed: $sub" }
}
Log ("C free: {0:N1} GB  D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
