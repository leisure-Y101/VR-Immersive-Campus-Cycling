# Elevated cleanup: remove Lenovo folders and SolidWorks remnants.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'elevated_cleanup.log'
if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$m) { Add-Content -Path $log -Value ("{0:HH:mm:ss}  {1}" -f (Get-Date), $m) -Encoding UTF8 }

Log "elevated=$(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))"

# kill anything still running from these folders
foreach ($root in @('C:\Program Files\Lenovo','C:\Program Files (x86)\Lenovo','D:\2022sw')) {
    foreach ($p in (Get-Process | Where-Object { $_.Path -and $_.Path.StartsWith($root) })) {
        Log "kill $($p.ProcessName) $($p.Id)"
        $p.Kill()
    }
}
Start-Sleep -Seconds 2

foreach ($dir in @('C:\Program Files\Lenovo','C:\Program Files (x86)\Lenovo','D:\2022sw')) {
    if (-not (Test-Path -LiteralPath $dir)) { Log "gone: $dir"; continue }
    $before = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
    Log ("delete {0} ({1:N2} GB)" -f $dir, ($before/1GB))
    Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $dir) {
        Get-ChildItem -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Attributes = 'Normal' } catch {} }
        Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $dir) {
        $after = (Get-ChildItem -LiteralPath $dir -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("  LEFTOVER {0:N2} GB" -f ($after/1GB))
    } else {
        Log "  removed"
    }
}
Log ("C free: {0:N1} GB   D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
