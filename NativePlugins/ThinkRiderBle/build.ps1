$ErrorActionPreference = 'Stop'

# Build ThinkRider BLE Android plugin into a jar for Unity.
# Requires Unity bundled OpenJDK 17 + Android SDK android.jar.

$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Data\PlaybackEngines\AndroidPlayer"
$javac = Join-Path $unity "OpenJDK\bin\javac.exe"
$jarTool = Join-Path $unity "OpenJDK\bin\jar.exe"
$androidJar = Join-Path $unity "SDK\platforms\android-36\android.jar"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src"
$classes = Join-Path $root "build\classes"
$outJar = Join-Path $root "build\ThinkRiderBle.jar"
$targetJar = "C:\Users\admin\freebike\Assets\Plugins\Android\ThinkRiderBle.jar"

foreach ($p in @($javac, $jarTool, $androidJar)) {
    if (-not (Test-Path $p)) { throw "Missing tool: $p" }
}

if (Test-Path (Join-Path $root "build")) { Remove-Item (Join-Path $root "build") -Recurse -Force }
New-Item -ItemType Directory -Path $classes -Force | Out-Null

$sources = Get-ChildItem $src -Recurse -Filter *.java | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) { throw "No Java sources found" }

& $javac -encoding UTF-8 -source 8 -target 8 -classpath "$androidJar" -d "$classes" $sources
if ($LASTEXITCODE -ne 0) { throw "javac failed" }

& $jarTool --create --file $outJar -C $classes .
if ($LASTEXITCODE -ne 0) { throw "jar failed" }

$pluginDir = Split-Path -Parent $targetJar
if (-not (Test-Path $pluginDir)) { New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null }
Copy-Item $outJar $targetJar -Force

Write-Output ("OK -> " + $targetJar)
Write-Output ("SIZE -> " + (Get-Item $targetJar).Length)
