# Elevated pass 5: remove Lenovo SLBSafe kernel driver, then delete leftovers.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'elevated_cleanup5.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

Log ("driver before: " + ((& sc.exe query SLBSafe 2>&1) -join ' '))
& sc.exe stop SLBSafe 2>&1 | ForEach-Object { Log ("  sc stop: " + $_) }
Start-Sleep -Seconds 2
& sc.exe delete SLBSafe 2>&1 | ForEach-Object { Log ("  sc delete: " + $_) }
Start-Sleep -Seconds 2
Log ("driver after: " + ((& sc.exe query SLBSafe 2>&1) -join ' '))

foreach ($svc in @('LenovoPcManagerService','LnvSvcFdn','LenovoFnAndFunctionKeys')) {
    & sc.exe stop $svc 2>&1 | Out-Null
    & sc.exe config $svc start= disabled 2>&1 | Out-Null
}

foreach ($dir in @('C:\Program Files (x86)\Lenovo\SLBrowser','C:\Program Files (x86)\Lenovo\PCManager','C:\Program Files (x86)\Lenovo\LeAppStore','C:\Program Files (x86)\Lenovo','C:\Program Files\Lenovo','D:\2022sw')) {
    if (-not (Test-Path -LiteralPath $dir)) { Log "gone: $dir"; continue }
    Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $dir) {
        $cnt = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object).Count
        $sz = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("  LEFTOVER {0} files={1} {2:N2}GB" -f $dir, $cnt, ($sz/1GB))
    } else { Log "  removed: $dir" }
}
Log ("C free: {0:N1} GB  D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
