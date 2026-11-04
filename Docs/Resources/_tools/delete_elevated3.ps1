# Elevated pass 3: diagnose and force-remove the last Lenovo folders.
$ErrorActionPreference = 'Continue'
$log = Join-Path $PSScriptRoot 'elevated_cleanup3.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

Log "elevated=$(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))"

# who has modules loaded from there
foreach ($p in Get-Process) {
    $mods = $p.Modules | Where-Object { $_.FileName -like 'C:\Program Files (x86)\Lenovo\*' -or $_.FileName -like 'C:\Program Files\Lenovo\*' }
    if ($mods) { Log "holder: $($p.ProcessName) ($($p.Id)) -> $($mods[0].FileName)" }
}

foreach ($dir in @('C:\Program Files (x86)\Lenovo\PCManager','C:\Program Files (x86)\Lenovo\SLBrowser','C:\Program Files (x86)\Lenovo\LeAppStore')) {
    if (-not (Test-Path -LiteralPath $dir)) { Log "gone: $dir"; continue }
    Log "--- $dir ---"
    $out = & cmd.exe /c "rd /s /q `"$dir`" 2>&1"
    if ($out) { Log ("  rd said: " + ($out -join ' | ')) }
    if (Test-Path -LiteralPath $dir) {
        $items = Get-ChildItem -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
        Log ("  still there, items=" + $items.Count)
        $sample = $items | Where-Object { -not $_.PSIsContainer } | Select-Object -First 3
        foreach ($s in $sample) { Log ("  sample: " + $s.FullName) }
    } else { Log "  removed" }
}

# final report
foreach ($d in @('C:\Program Files (x86)\Lenovo','C:\Program Files\Lenovo','D:\2022sw')) {
    if (Test-Path -LiteralPath $d) {
        $sz = (Get-ChildItem -LiteralPath $d -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("EXISTS {0}  {1:N2} GB" -f $d, ($sz/1GB))
    } else { Log "GONE $d" }
}
Log ("C free: {0:N1} GB  D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
