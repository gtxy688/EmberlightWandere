# EmberlightWandere - Unity batchmode compile verification
#
# MUST be run from a normal (non-sandboxed) terminal: Unity's licensing client needs
# named pipes, and a sandboxed shell makes it fail with
#   [Licensing::IpcConnector] Connection to channel LicenseClient-<n> refused
#   IPC channel to LicensingClient doesn't exist; aborting
#   Application will terminate with return code 199
#
# REQUIRES the Unity editor to be CLOSED, otherwise Unity crashes with
# HandleProjectAlreadyOpenInAnotherInstance.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File Tools\unity-batch-verify.ps1
#
# Exit code: 0 = Unity imported + compiled the project cleanly.

[CmdletBinding()]
param(
    [string]$UnityExe = 'E:\Unity\Unity\2022.3.62f3\Editor\Unity.exe',
    [string]$ProjectPath = '',
    [string]$LogPath = 'Library/BatchCompile/unity-compile.log'
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectPath) {
    $scriptDir = $PSScriptRoot
    if (-not $scriptDir) { $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition }
    if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
    $ProjectPath = Split-Path -Parent $scriptDir
}

if (-not (Test-Path $UnityExe)) { Write-Host "Unity.exe not found: $UnityExe"; exit 2 }

$running = Get-Process -Name Unity -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Unity editor is RUNNING (PID $($running.Id -join ', '))." -ForegroundColor Red
    Write-Host "Close it first: batchmode aborts with HandleProjectAlreadyOpenInAnotherInstance."
    exit 3
}

$logFull = Join-Path $ProjectPath $LogPath
New-Item -ItemType Directory -Force -Path (Split-Path $logFull -Parent) | Out-Null
if (Test-Path $logFull) { Remove-Item $logFull -Force }

# Record the produced assemblies before the run so compilation is proven by the ARTIFACT, not by
# log wording (Unity's "compiled" lines differ between the incremental and full paths).
# 2026-10-03: the project moved into its own asmdefs, so `Assembly-CSharp.dll` is no longer
# produced. Watch the project's own assemblies instead, and keep the legacy one as a fallback.
$asmPaths = @(
    (Join-Path $ProjectPath 'Library/ScriptAssemblies/Emberlight.Runtime.dll'),
    (Join-Path $ProjectPath 'Library/ScriptAssemblies/Assembly-CSharp.dll')
)
$asmPath = $asmPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $asmPath) { $asmPath = $asmPaths[0] }
$beforeTicks = if (Test-Path $asmPath) { (Get-Item $asmPath).LastWriteTimeUtc.Ticks } else { 0 }

Write-Host "Unity   : $UnityExe"
Write-Host "Project : $ProjectPath"
Write-Host "Log     : $logFull"
Write-Host "Importing + compiling (this takes a few minutes)..."
Write-Host ''

# -quit + an existing project forces a full asset import and script compilation.
& $UnityExe -batchmode -nographics -quit -projectPath $ProjectPath -logFile $logFull
$code = $LASTEXITCODE
if ($null -eq $code) { $code = 0 }

# Unity can still be flushing the log file when the process returns, so wait for it to settle.
$deadline = (Get-Date).AddSeconds(20)
while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFull) -and (Get-Item $logFull).Length -gt 0) {
        Start-Sleep -Milliseconds 700
        $len1 = (Get-Item $logFull).Length
        Start-Sleep -Milliseconds 700
        if ((Get-Item $logFull).Length -eq $len1) { break }
    }
    Start-Sleep -Milliseconds 300
}

if (-not (Test-Path $logFull) -or (Get-Item $logFull).Length -eq 0) {
    Write-Host "Unity wrote no log at $logFull - it most likely never started." -ForegroundColor Red
    Write-Host "Run this from a normal terminal with Unity closed." -ForegroundColor Yellow
    exit 2
}

$log = @(Get-Content $logFull)

$csErrors = @($log | Where-Object { $_ -match 'error CS\d+' })
$compileErrors = @($log | Where-Object { $_ -match 'Compilation failed|Scripts have compiler errors' })
$licenseFailure = @($log | Where-Object { $_ -match "LicensingClient doesn't exist|return code 199" })

# Unity can still be copying the build artifacts out of Bee when the process exits, so poll for
# the assembly to settle before judging "did it build". Checking once here produced a false
# "produced no Assembly-CSharp.dll" on a run that had compiled fine.
$deadline = (Get-Date).AddSeconds(30)
$afterTicks = if (Test-Path $asmPath) { (Get-Item $asmPath).LastWriteTimeUtc.Ticks } else { 0 }
while ((Get-Date) -lt $deadline) {
    if (-not (Test-Path $asmPath)) { Start-Sleep -Milliseconds 400; continue }
    $t1 = (Get-Item $asmPath).LastWriteTimeUtc.Ticks
    Start-Sleep -Milliseconds 600
    $afterTicks = (Get-Item $asmPath).LastWriteTimeUtc.Ticks
    if ($afterTicks -eq $t1) { break }
}
$rebuilt = ($afterTicks -ne 0) -and ($afterTicks -ne $beforeTicks)

Write-Host ''
if ($licenseFailure.Count -gt 0) {
    Write-Host 'FAILED: Unity licensing client could not start.' -ForegroundColor Red
    Write-Host 'Run this script from a normal terminal, not from inside a sandboxed shell.' -ForegroundColor Yellow
    exit 199
}

if ($csErrors.Count -gt 0 -or $compileErrors.Count -gt 0) {
    Write-Host "COMPILE FAILED: $($csErrors.Count) C# error line(s)" -ForegroundColor Red
    $csErrors | Select-Object -First 40 | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}

if ($code -ne 0) {
    Write-Host "Unity exited with code $code" -ForegroundColor Red
    Write-Host "Last 25 log lines:"
    $log | Select-Object -Last 25 | ForEach-Object { Write-Host $_ }
    exit $code
}

if ($rebuilt) {
    $asm = Get-Item $asmPath
    Write-Host "COMPILE OK - $($asm.Name) rebuilt ($($asm.Length) bytes @ $($asm.LastWriteTime))." -ForegroundColor Green
} else {
    $asm = if (Test-Path $asmPath) { Get-Item $asmPath } else { $null }
    if ($asm) {
        Write-Host "COMPILE OK (incremental) - $($asm.Name) unchanged ($($asm.Length) bytes @ $($asm.LastWriteTime))." -ForegroundColor Green
        Write-Host 'Nothing needed recompiling. To force a full rebuild: close Unity,' -ForegroundColor Yellow
        Write-Host 'delete Library/ScriptAssemblies, then re-run this script.' -ForegroundColor Yellow
    } else {
        Write-Host "WARNING: Unity exited cleanly but produced no $([System.IO.Path]::GetFileName($asmPaths[0]))." -ForegroundColor Yellow
        Write-Host 'If the project has no asmdef, check Assembly-CSharp.dll instead.' -ForegroundColor Yellow
    }
}

Write-Host 'UNITY COMPILE OK - project imported with no C# errors.' -ForegroundColor Green
Write-Host ''
Write-Host 'Still to do by hand (cannot be automated):'
Write-Host '  R1  Enter Play: loading -> menu -> four setup steps -> combat -> card -> pause -> result -> camp'
Write-Host '  R2  Take repeated contact damage: ~0.65s i-frames, no chain-kill'
Write-Host '  R5  Pick a new weapon: exactly one compensation card, no chain'
Write-Host '  R8  Drag lower half of screen: joystick follows; Esc pauses'
Write-Host '  See Docs/architecture/00-composition-root-test.md'
exit 0
