# Remove Lenovo leftovers + SOLIDWORKS remnants, and clean orphan uninstall entries.
$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'cleanup2.log'
if (Test-Path $log) { Remove-Item $log -Force }

function Log([string]$m) {
    $line = "{0:HH:mm:ss}  {1}" -f (Get-Date), $m
    Write-Output $line
    Add-Content -Path $log -Value $line -Encoding UTF8
}

Log "=== START ==="
$freeBefore = (Get-PSDrive C).Free + (Get-PSDrive D).Free

$targets = @('C:\Program Files\Lenovo', 'C:\Program Files (x86)\Lenovo')

# stop Lenovo services
foreach ($svc in @('LenovoVantageService','LenovoPcManagerService','LenovoServiceAS','LenovoProcessManagement',
                   'SmartSense','LISFService','LenovoSLBrowserElevationService','LnvVCamInstaller','HRWSCCtrl')) {
    $s = Get-Service -Name $svc
    if ($s -and $s.Status -eq 'Running') {
        Log "  stop service: $svc"
        Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
    }
}

# kill processes running from those folders
foreach ($proc in (Get-Process | Where-Object { $_.Path -and ($targets | Where-Object { $_.Path.StartsWith($_) }) })) {
    Log "  kill: $($proc.ProcessName) ($($proc.Id))"
    $proc.Kill()
}
Start-Sleep -Seconds 3

function RemoveDir([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { Log "  [gone] $path"; return }
    Log "  delete: $path"
    Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $path) {
        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Attributes = 'Normal' } catch {} }
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $path) {
        $left = (Get-ChildItem -LiteralPath $path -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Log ("  [leftover] {0} ({1:N2} GB)" -f $path, ($left/1GB))
    } else {
        Log "  [done] removed"
    }
}

foreach ($t in $targets) { RemoveDir $t }
RemoveDir 'D:\2022sw'

# clean orphan uninstall registry entries for removed apps
$patterns = 'Lenovo Speech','LenovoBaiying','Lenovo Vantage Service','SOLIDWORKS','CCS Theia','Keil'
$roots = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall','HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall')
foreach ($r in $roots) {
    foreach ($k in (Get-ChildItem $r -ErrorAction SilentlyContinue)) {
        $props = Get-ItemProperty -LiteralPath $k.PSPath
        if (-not $props.DisplayName) { continue }
        foreach ($p in $patterns) {
            if ($props.DisplayName -like "*$p*") {
                Log "  remove registry entry: $($props.DisplayName)"
                Remove-Item -LiteralPath $k.PSPath -Recurse -Force -ErrorAction SilentlyContinue
                break
            }
        }
    }
}

$freeAfter = (Get-PSDrive C).Free + (Get-PSDrive D).Free
Log ("freed this pass: {0:N2} GB" -f (($freeAfter - $freeBefore)/1GB))
Log ("C free: {0:N1} GB   D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
foreach ($p in $targets + @('D:\2022sw','D:\keil5','D:\CCSTheia1.4.1.00001_win64')) {
    Log ("  exists={0}  {1}" -f (Test-Path -LiteralPath $p), $p)
}
