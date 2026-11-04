$ErrorActionPreference = 'Stop'
$Project = 'D:\freebike'
$Artifacts = 'D:\freebikeapk'
$Migration = Join-Path $Project 'Docs\Migration'
$Log = Join-Path $Migration 'organize_2026-10-29.log'

function EnsureDir($p) {
    if (-not (Test-Path -LiteralPath $p)) {
        New-Item -ItemType Directory -Path $p -Force | Out-Null
    }
}

function MovePath($src, $dst) {
    if (-not (Test-Path -LiteralPath $src)) {
        Write-Log "MISSING $src"
        return
    }
    EnsureDir (Split-Path -Parent $dst)
    if (Test-Path -LiteralPath $dst) {
        $dstItem = Get-Item -LiteralPath $dst
        $srcItem = Get-Item -LiteralPath $src
        if ($dstItem.PSIsContainer -and $srcItem.PSIsContainer -and -not (Get-ChildItem -LiteralPath $dst -Force -ErrorAction SilentlyContinue)) {
            Remove-Item -LiteralPath $dst -Force
        } else {
            throw "DEST EXISTS: $dst"
        }
    }
    Move-Item -LiteralPath $src -Destination $dst -Force
    Write-Log "MOVED $src -> $dst"
}

function CopyPath($src, $dst) {
    if (-not (Test-Path -LiteralPath $src)) {
        Write-Log "MISSING $src"
        return
    }
    EnsureDir (Split-Path -Parent $dst)
    Copy-Item -LiteralPath $src -Destination $dst -Force
    Write-Log "COPIED $src -> $dst"
}

function RemovePath($p) {
    if (Test-Path -LiteralPath $p) {
        Remove-Item -LiteralPath $p -Recurse -Force
        Write-Log "REMOVED $p"
    }
}

function Write-Log($text) {
    Add-Content -LiteralPath $Log -Value ("[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $text) -Encoding UTF8
    Write-Output $text
}

EnsureDir $Migration
EnsureDir (Join-Path $Project 'Docs')
EnsureDir (Join-Path $Project 'Design')
EnsureDir (Join-Path $Project 'Media')
EnsureDir (Join-Path $Project 'Releases')
EnsureDir (Join-Path $Project 'Tools')
Write-Log 'START organize'

# ---------------------------------------------------------------------------
# Docs / report / logs
# ---------------------------------------------------------------------------
EnsureDir (Join-Path $Project 'Docs\Reports')
EnsureDir (Join-Path $Project 'Docs\Reports\archive')
EnsureDir (Join-Path $Project 'Docs\Reports\media')
EnsureDir (Join-Path $Project 'Docs\WorkLogs')
EnsureDir (Join-Path $Project 'Docs\AI')
EnsureDir (Join-Path $Project 'Docs\Handoff')
EnsureDir (Join-Path $Project 'Docs\Resources')
EnsureDir (Join-Path $Project 'Docs\Logs')
EnsureDir (Join-Path $Project 'Docs\RuntimeReports')

# Latest report from Desktop wins.
CopyPath 'D:\Users\admin\Desktop\FreeBike_工作报告_2026-10-29.docx' (Join-Path $Project 'Docs\Reports\FreeBike_工作报告_2026-10-29.docx')
if (Test-Path -LiteralPath (Join-Path $Artifacts 'FreeBike_工作报告_2026-10-29.docx')) {
    MovePath (Join-Path $Artifacts 'FreeBike_工作报告_2026-10-29.docx') (Join-Path $Project 'Docs\Reports\archive\FreeBike_工作报告_2026-10-29_freebikeapk.docx')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts '工作日志')) {
    MovePath (Join-Path $Artifacts '工作日志') (Join-Path $Project 'Docs\WorkLogs')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts 'AI文档')) {
    MovePath (Join-Path $Artifacts 'AI文档') (Join-Path $Project 'Docs\AI')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts 'Resources')) {
    MovePath (Join-Path $Artifacts 'Resources') (Join-Path $Project 'Docs\Resources')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts '交付')) {
    MovePath (Join-Path $Artifacts '交付') (Join-Path $Project 'Docs\Handoff')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts 'CyclingReport')) {
    MovePath (Join-Path $Artifacts 'CyclingReport') (Join-Path $Project 'Docs\RuntimeReports\CyclingReport')
}
if (Test-Path -LiteralPath (Join-Path $Artifacts '_docx_media')) {
    MovePath (Join-Path $Artifacts '_docx_media') (Join-Path $Project 'Docs\Reports\media')
}

# Logs from artifacts root and from Unity project.
$logFiles = @(
    'pico_apk_build.log','pico_apk_build2.log','pico_apk_build3.log',
    'windows_build.log','freebike_project_move.log',
    'unity_app_import.log','unity_final_check.log','pico_migration_import.log','unity_validate2.log'
)
foreach ($f in $logFiles) {
    $src = Join-Path $Artifacts $f
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project ('Docs\Logs\' + $f)) }
}
$projLog = Join-Path $Project 'CampusRideUIBuilder.log'
if (Test-Path -LiteralPath $projLog) { MovePath $projLog (Join-Path $Project 'Docs\Logs\CampusRideUIBuilder.log') }
$editorLogs = Join-Path $Project 'Logs'
if (Test-Path -LiteralPath $editorLogs) { MovePath $editorLogs (Join-Path $Project 'Docs\Logs\UnityLogs') }

# Report build/source helper scripts.
EnsureDir (Join-Path $Project 'Docs\Reports\_scripts')
foreach ($f in @('make_freebike_report.py','edit_report_addendum.py','fix_report_tables.py','resize_report_tables.py','report_dump.txt')) {
    $src = Join-Path $Artifacts $f
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project ('Docs\Reports\_scripts\' + $f)) }
}

# ---------------------------------------------------------------------------
# Design
# ---------------------------------------------------------------------------
EnsureDir (Join-Path $Project 'Design\UI')
EnsureDir (Join-Path $Project 'Design\References')
EnsureDir (Join-Path $Project 'Design\Models')
if (Test-Path -LiteralPath (Join-Path $Artifacts 'UI')) { MovePath (Join-Path $Artifacts 'UI') (Join-Path $Project 'Design\UI') }
if (Test-Path -LiteralPath (Join-Path $Artifacts 'refs')) { MovePath (Join-Path $Artifacts 'refs') (Join-Path $Project 'Design\References') }
if (Test-Path -LiteralPath (Join-Path $Artifacts 'models')) { MovePath (Join-Path $Artifacts 'models') (Join-Path $Project 'Design\Models') }
$pycache = Join-Path $Project 'Design\UI\__pycache__'
if (Test-Path -LiteralPath $pycache) { RemovePath $pycache }

# ---------------------------------------------------------------------------
# Tools
# ---------------------------------------------------------------------------
EnsureDir (Join-Path $Project 'Tools\A1')
EnsureDir (Join-Path $Project 'Tools\deps')
EnsureDir (Join-Path $Project 'Tools\ffmpeg')
EnsureDir (Join-Path $Project 'Tools\compilecheck')
foreach ($f in @('a1_bridge.py','inspect_a1.py','scan_ble.py','启动骑行台桥接.cmd','启动Demo_等待SteamVR.cmd')) {
    $src = Join-Path $Artifacts $f
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project ('Tools\A1\' + $f)) }
}
foreach ($p in @('pydeps','.pydeps-ble')) {
    $src = Join-Path $Artifacts $p
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project ('Tools\deps\' + $p)) }
}
foreach ($p in @('_tools')) {
    $src = Join-Path $Artifacts $p
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project 'Tools\ffmpeg') }
}
if (Test-Path -LiteralPath (Join-Path $Artifacts 'compilecheck')) { MovePath (Join-Path $Artifacts 'compilecheck') (Join-Path $Project 'Tools\compilecheck') }
$abak = Join-Path $Artifacts 'a1_bridge.py.bak'
if (Test-Path -LiteralPath $abak) { RemovePath $abak }

# ---------------------------------------------------------------------------
# Media
# ---------------------------------------------------------------------------
EnsureDir (Join-Path $Project 'Media\Video')
EnsureDir (Join-Path $Project 'Media\Stills')
$mediaMap = @(
    @('D:\freebikeapk\360vidio\pro2\6k60fps.mp4', (Join-Path $Project 'Media\Video\6k60fps.mp4')),
    @('D:\freebikeapk\360vidio\pro2\6k60fps_hevc.mp4', (Join-Path $Project 'Media\Video\6k60fps_hevc.mp4')),
    @('D:\freebikeapk\360vidio\pro2\PIC_20261026_071025_26_10_26_11_33_08_output_1.jpg', (Join-Path $Project 'Media\Stills\homepage_source.jpg')),
    @('D:\freebikeapk\360vidio\pro2\_mono_contact.jpg', (Join-Path $Project 'Media\Stills\mono_contact.jpg'))
)
foreach ($m in $mediaMap) {
    if (Test-Path -LiteralPath $m[0]) { MovePath $m[0] $m[1] }
}

# ---------------------------------------------------------------------------
# Releases: latest two only
# ---------------------------------------------------------------------------
EnsureDir (Join-Path $Project 'Releases\Windows')
EnsureDir (Join-Path $Project 'Releases\Android')
EnsureDir (Join-Path $Project 'Releases\Benchmark')
if (Test-Path -LiteralPath (Join-Path $Artifacts 'demo')) { MovePath (Join-Path $Artifacts 'demo') (Join-Path $Project 'Releases\Windows\demo_2026-10-27') }
if (Test-Path -LiteralPath (Join-Path $Artifacts 'freebike2.0')) { MovePath (Join-Path $Artifacts 'freebike2.0') (Join-Path $Project 'Releases\Windows\freebike2.0_2026-10-26') }
foreach ($apk in @('testbike1.9.apk','testbike1.8.apk')) {
    $src = Join-Path $Artifacts ('版本更新\' + $apk)
    if (Test-Path -LiteralPath $src) { MovePath $src (Join-Path $Project ('Releases\Android\' + $apk)) }
}
$benchApk = Join-Path $Artifacts 'Builds\PicoVideoBenchmark.apk'
if (Test-Path -LiteralPath $benchApk) { MovePath $benchApk (Join-Path $Project 'Releases\Benchmark\PicoVideoBenchmark.apk') }
$benchCmd = Join-Path $Artifacts 'Builds\install_benchmark.cmd'
if (Test-Path -LiteralPath $benchCmd) { MovePath $benchCmd (Join-Path $Project 'Releases\Benchmark\install_benchmark.cmd') }

# ---------------------------------------------------------------------------
# Remove old build/backup directories (keep latest two)
# ---------------------------------------------------------------------------
RemovePath (Join-Path $Artifacts 'CampusRideDemo')
RemovePath (Join-Path $Artifacts '版本更新')
RemovePath (Join-Path $Artifacts 'Builds')
RemovePath (Join-Path $Artifacts '_backup_20261027')

# Remove old media after selected files moved.
RemovePath (Join-Path $Artifacts '360vidio')

# ---------------------------------------------------------------------------
# Unity generated/unused cleanup
# ---------------------------------------------------------------------------
RemovePath (Join-Path $Project 'Assets\Video')
RemovePath (Join-Path $Project 'Assets\_Recovery')
RemovePath (Join-Path $Project 'build')
RemovePath (Join-Path $Project '_codex_backup_20261026-032408')
RemovePath (Join-Path $Project 'obj')
RemovePath (Join-Path $Project '.vs')

# ---------------------------------------------------------------------------
# Staging files -> root
# ---------------------------------------------------------------------------
CopyPath (Join-Path $Artifacts 'README_github.md') (Join-Path $Project 'README.md')
CopyPath (Join-Path $Artifacts '.gitignore') (Join-Path $Project '.gitignore')
CopyPath (Join-Path $Artifacts '.gitattributes') (Join-Path $Project '.gitattributes')
CopyPath (Join-Path $Artifacts 'Media_README.md') (Join-Path $Project 'Media\README.md')
CopyPath (Join-Path $Artifacts 'Releases_README.md') (Join-Path $Project 'Releases\README.md')

# ---------------------------------------------------------------------------
# Update hardcoded paths in Unity sources and scene
# ---------------------------------------------------------------------------
$app = Join-Path $Project 'Assets\Scripts\App\CampusRideApp.cs'
if (Test-Path -LiteralPath $app) {
    $t = Get-Content -LiteralPath $app -Raw -Encoding UTF8
    $t = $t.Replace('D:\freebikeapk\360vidio\daytimetest.mp4', 'D:\freebike\Media\Video\6k60fps_hevc.mp4')
    $t = $t.Replace('daytimetest.mp4', '6k60fps_hevc.mp4')
    Set-Content -LiteralPath $app -Value $t -Encoding UTF8 -NoNewline
    Write-Log 'UPDATED CampusRideApp.cs paths'
}

$scene = Join-Path $Project 'Assets\Scenes\panoTest.unity'
if (Test-Path -LiteralPath $scene) {
    $t = Get-Content -LiteralPath $scene -Raw -Encoding UTF8
    $t = $t.Replace('file:///D:/freebikeapk/360vidio/daytimetest.mp4', 'file:///D:/freebike/Media/Video/6k60fps_hevc.mp4')
    $t = $t.Replace('D:\freebikeapk\360vidio\pro2\6k60fps_4k.mp4', 'D:\freebike\Media\Video\6k60fps_hevc.mp4')
    $t = $t.Replace('pro2/6k60fps_4k.mp4', '6k60fps_hevc.mp4')
    Set-Content -LiteralPath $scene -Value $t -Encoding UTF8 -NoNewline
    Write-Log 'UPDATED panoTest.unity paths'
}

$buildWin = Join-Path $Project 'Assets\Editor\BuildWindowsCampusRide.cs'
if (Test-Path -LiteralPath $buildWin) {
    $t = Get-Content -LiteralPath $buildWin -Raw -Encoding UTF8
    $t = $t.Replace('D:\freebikeapk\CampusRideDemo', 'D:\freebike\Releases\Windows\CampusRideDemo')
    Set-Content -LiteralPath $buildWin -Value $t -Encoding UTF8 -NoNewline
    Write-Log 'UPDATED BuildWindowsCampusRide.cs output path'
}

$buildBench = Join-Path $Project 'Assets\Editor\PicoBenchmarkBuilder.cs'
if (Test-Path -LiteralPath $buildBench) {
    $t = Get-Content -LiteralPath $buildBench -Raw -Encoding UTF8
    $t = $t.Replace('D:\freebikeapk\Builds', 'D:\freebike\Releases\Benchmark')
    Set-Content -LiteralPath $buildBench -Value $t -Encoding UTF8 -NoNewline
    Write-Log 'UPDATED PicoBenchmarkBuilder.cs output path'
}

$launcher = Join-Path $Project 'Tools\A1\启动Demo_等待SteamVR.cmd'
if (Test-Path -LiteralPath $launcher) {
    $t = Get-Content -LiteralPath $launcher -Raw -Encoding UTF8
    $t = $t.Replace('D:\freebikeapk\demo\freebike.exe', 'D:\freebike\Releases\Windows\demo_2026-10-27\freebike.exe')
    Set-Content -LiteralPath $launcher -Value $t -Encoding UTF8 -NoNewline
    Write-Log 'UPDATED launcher path'
}

# ---------------------------------------------------------------------------
# Manifest
# ---------------------------------------------------------------------------
$manifest = @'
# 2026-10-29 reorganization

Project root: D:\freebike
Old artifacts root: D:\freebikeapk

Kept:
- Unity project: Assets, Packages, ProjectSettings
- Docs: AI, WorkLogs, Reports, Handoff, Resources, Logs, RuntimeReports
- Design: UI previews, references, models
- Tools: A1 bridge, deps, ffmpeg, compilecheck
- Media: 6k60fps.mp4, 6k60fps_hevc.mp4, homepage source, mono contact
- Releases/Windows: demo_2026-10-27, freebike2.0_2026-10-26
- Releases/Android: testbike1.9.apk, testbike1.8.apk
- Releases/Benchmark: PicoVideoBenchmark.apk

Removed:
- CampusRideDemo (older Windows)
- 版本更新 old APKs 1.1-1.7, freebike1.0
- Builds backup
- _backup_20261027
- 360vidio old videos and 29 unused stills
- Assets/Video, Assets/_Recovery, build, obj, .vs

Not in Git:
- Library, Media, Releases, Assets/Video, StreamingAssets/Videos, deps, ffmpeg
'@
Set-Content -LiteralPath (Join-Path $Migration 'README.md') -Value $manifest -Encoding UTF8
Write-Log 'DONE organize'

Write-Output 'ORGANIZE COMPLETE'
