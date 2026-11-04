# Elevated pass 4: take ownership of Lenovo leftovers, then delete. Quiet logging.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'elevated_cleanup4.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

foreach ($dir in @('C:\Program Files (x86)\Lenovo\SLBrowser','C:\Program Files (x86)\Lenovo\LeAppStore','C:\Program Files (x86)\Lenovo\PCManager','D:\2022sw')) {
    if (-not (Test-Path -LiteralPath $dir)) { Log "gone: $dir"; continue }
    Log "takeown: $dir"
    $t = & takeown.exe /f "$dir" /r /d y 2>&1
    Log ("  takeown lines=" + ($t | Measure-Object).Count)
    $i = & icacls.exe "$dir" /grant "*S-1-5-32-544:(OI)(CI)F" /t /c /q 2>&1
    Log ("  icacls lines=" + ($i | Measure-Object).Count)
    Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $dir) {
        $cnt = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object).Count
        $sz = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("  LEFTOVER files={0} size={1:N2}GB" -f $cnt, ($sz/1GB))
    } else { Log "  removed" }
}

foreach ($d in @('C:\Program Files (x86)\Lenovo','C:\Program Files\Lenovo','D:\2022sw')) {
    $ok = Test-Path -LiteralPath $d
    $sz = 0
    if ($ok) { $sz = (Get-ChildItem -LiteralPath $d -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum }
    $state = 'GONE'
    if ($ok) { $state = 'EXISTS' }
    Log ("{0} {1:N2} GB  {2}" -f $state, ($sz/1GB), $d)
}
Log ("C free: {0:N1} GB  D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
