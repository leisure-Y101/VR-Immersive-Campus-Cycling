$ErrorActionPreference = 'Continue'
$out = Join-Path $PSScriptRoot 'diag6.log'
if (Test-Path $out) { Remove-Item $out -Force }
function Log([string]$m) { Add-Content -Path $out -Value $m -Encoding UTF8 }

$dir = 'C:\Program Files (x86)\Lenovo\SLBrowser'
Log "== test create in $dir =="
try { Set-Content -LiteralPath (Join-Path $dir 'zz_test.txt') -Value 'x' -ErrorAction Stop; Log "create: OK" } catch { Log ("create: FAIL " + $_.Exception.Message) }
Remove-Item -LiteralPath (Join-Path $dir 'zz_test.txt') -Force -ErrorAction SilentlyContinue

Log "== test delete existing file =="
$f = Join-Path $dir 'SLBrowser.exe'
if (Test-Path -LiteralPath $f) {
    $r = & cmd.exe /c "del /f /q `"$f`" 2>&1"
    Log ("del result: " + ($r -join ' | ') + " exists=" + (Test-Path -LiteralPath $f))
}

Log "== processes with modules from Lenovo dirs =="
foreach ($p in Get-Process) {
    $m = $p.Modules | Where-Object { $_.FileName -like '*\Lenovo\*' } | Select-Object -First 1
    if ($m) { Log ("  {0} ({1}) -> {2}" -f $p.ProcessName, $p.Id, $m.FileName) }
}

Log "== solidworks leftovers =="
$sd = 'D:\2022sw'
foreach ($f2 in (Get-ChildItem -LiteralPath $sd -Recurse -File -Force -ErrorAction SilentlyContinue)) {
    $r2 = & cmd.exe /c "del /f /q `"$($f2.FullName)`" 2>&1"
    Log ("  {0} : {1}" -f $f2.Name, ($r2 -join ' | '))
}
Log ("D:\2022sw exists=" + (Test-Path -LiteralPath $sd))
