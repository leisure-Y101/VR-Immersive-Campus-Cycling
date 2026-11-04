# Restore Lenovo service startup types and reinstall Lenovo PC Manager.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'reinstall_pcm.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

$autos = @('LenovoPcManagerService','LenovoVantageService','SmartSense','LenovoServiceAS','LenovoProcessManagement','LnvVCamInstaller','SLBService','HRWSCCtrl')
foreach ($s in $autos) {
    if (Get-Service -Name $s) {
        & sc.exe config $s start= auto | Out-Null
        Log "service $s -> auto"
    }
}
$manual = @('LenovoSLBrowserElevationService','LnvSvcFdn')
foreach ($s in $manual) {
    if (Get-Service -Name $s) {
        & sc.exe config $s start= demand | Out-Null
        Log "service $s -> demand"
    }
}

$exe = 'C:\Users\admin\Downloads\lenovopcmanager_apps.exe'
Log "installer exists: $(Test-Path -LiteralPath $exe)"
$p = Start-Process -FilePath $exe -ArgumentList '/S' -PassThru -WindowStyle Hidden
if ($p.WaitForExit(600000)) { Log "installer exit=$($p.ExitCode)" } else { Log "installer timeout (still running), will keep waiting in background"; }
Start-Sleep -Seconds 5

foreach ($d in @('C:\Program Files (x86)\Lenovo\PCManager','C:\Program Files\Lenovo')) {
    Log ("{0} exists={1}" -f $d, (Test-Path -LiteralPath $d))
}
$svc = Get-Service LenovoPcManagerService
if ($svc) { Log "LenovoPcManagerService status=$($svc.Status) startType=$($svc.StartType)" }
Log ("C free: {0:N1} GB" -f ((Get-PSDrive C).Free/1GB))
