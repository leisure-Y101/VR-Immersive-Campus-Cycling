# Uninstall Lenovo bundle / CCS Theia / SOLIDWORKS 2022 / Keil5
# ASCII-only to avoid PowerShell 5.1 ANSI parsing issues.

$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'uninstall.log'
if (Test-Path $log) { Remove-Item $log -Force }

function Log([string]$m) {
    $line = "{0:HH:mm:ss}  {1}" -f (Get-Date), $m
    Write-Output $line
    Add-Content -Path $log -Value $line -Encoding UTF8
}

function RunAndWait([string]$exe, [string[]]$args, [int]$timeoutSec = 900) {
    if (-not (Test-Path -LiteralPath $exe)) { Log "  [skip] not found: $exe"; return $false }
    Log "  run: $exe $($args -join ' ')"
    try {
        $p = Start-Process -FilePath $exe -ArgumentList $args -PassThru -WindowStyle Hidden
        if (-not $p.WaitForExit($timeoutSec * 1000)) {
            Log "  [timeout ${timeoutSec}s] kill PID=$($p.Id)"
            try { $p.Kill() } catch {}
            return $false
        }
        Log "  exit: $($p.ExitCode)"
        return ($p.ExitCode -eq 0 -or $p.ExitCode -eq 3010 -or $p.ExitCode -eq 1641)
    } catch {
        Log "  [fail] $($_.Exception.Message)"
        return $false
    }
}

function RemoveDir([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { Log "  [gone] $path"; return 0 }
    $bytes = (Get-ChildItem -LiteralPath $path -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
    Log "  delete dir: $path ($([math]::Round($bytes/1GB,2)) GB)"
    Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $path) {
        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Attributes = 'Normal' }
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $path) { Log "  [leftover] $path still exists"; return 0 }
    Log "  [done] removed"
    return $bytes
}

$freeBefore = (Get-PSDrive C).Free + (Get-PSDrive D).Free
Log "=== START: C=$([math]::Round((Get-PSDrive C).Free/1GB,1))GB D=$([math]::Round((Get-PSDrive D).Free/1GB,1))GB ==="

# ---------- 1. Lenovo ----------
Log "[1/4] Lenovo bundle"
$lenovoUninstallers = @()
foreach ($root in @('C:\Program Files\Lenovo', 'C:\Program Files (x86)\Lenovo')) {
    if (Test-Path -LiteralPath $root) {
        $lenovoUninstallers += Get-ChildItem -LiteralPath $root -Recurse -Filter 'unins000.exe' -File -ErrorAction SilentlyContinue
        $lenovoUninstallers += Get-ChildItem -LiteralPath $root -Recurse -Filter 'Uninstall.exe' -File -ErrorAction SilentlyContinue
    }
}
foreach ($u in ($lenovoUninstallers | Sort-Object -Unique FullName)) {
    Log "  component uninstaller: $($u.FullName)"
    RunAndWait $u.FullName @('/SILENT', '/NORESTART') 180 | Out-Null
}
foreach ($d in @('C:\Program Files\Lenovo', 'C:\Program Files (x86)\Lenovo')) { RemoveDir $d | Out-Null }

# ---------- 2. CCS Theia ----------
Log "[2/4] CCS Theia"
$ccs = 'D:\CCSTheia1.4.1.00001_win64'
RunAndWait (Join-Path $ccs 'ccs\uninstall_ccs.exe') @('--mode', 'unattended') 900 | Out-Null
RemoveDir $ccs | Out-Null

# ---------- 3. SOLIDWORKS 2022 ----------
Log "[3/4] SOLIDWORKS 2022 (per-component MSI silent uninstall)"
$sw = @(
    '{26EA0056-4BAD-4F9E-BDCE-A72E25C7D06D}',
    '{58BEDD50-0DAB-4BBD-AF66-62A93C77F8AF}',
    '{A7D46D29-7CBB-4CD2-8DFD-5986F4446AB4}',
    '{273E0863-61E6-4E82-A2FF-D28DB44D6471}',
    '{46053718-2931-47EA-B678-6DF08370F2D4}',
    '{54DF002F-CDCE-40B2-8E2A-E294333EE6EB}',
    '{770F37F3-8897-4C37-B7F2-B1AA5FC825C7}',
    '{E5A7DAD6-65DE-4DDA-8240-D49168365FF2}',
    '{EB00B3B9-B938-4123-9BDC-F7CD57128E16}',
    '{E1C7FBC9-5D5C-4C5F-BE99-185E8576BC5B}',
    '{E3B4130E-6066-4613-918C-6EB5E7B68D8C}'
)
foreach ($g in $sw) {
    Log "  MSI $g"
    RunAndWait 'msiexec.exe' @('/x', $g, '/qn', '/norestart') 900 | Out-Null
}
RemoveDir 'D:\2022sw' | Out-Null
RemoveDir 'C:\WINDOWS\SolidWorks' | Out-Null

# ---------- 4. Keil5 ----------
Log "[4/4] Keil5"
$keilExe = 'D:\keil5\NEWkeil\Uninstall.exe'
if (Test-Path -LiteralPath $keilExe) { RunAndWait $keilExe @('/S') 300 | Out-Null }
RemoveDir 'D:\keil5' | Out-Null

# ---------- summary ----------
$freeAfter = (Get-PSDrive C).Free + (Get-PSDrive D).Free
Log "=== END ==="
Log ("freed: {0:N2} GB" -f (($freeAfter - $freeBefore) / 1GB))
Log ("C free: {0:N1} GB   D free: {1:N1} GB" -f ((Get-PSDrive C).Free/1GB), ((Get-PSDrive D).Free/1GB))
foreach ($p in @('C:\Program Files\Lenovo','C:\Program Files (x86)\Lenovo','D:\CCSTheia1.4.1.00001_win64','D:\2022sw','D:\keil5')) {
    Log ("  exists={0}  {1}" -f (Test-Path -LiteralPath $p), $p)
}
