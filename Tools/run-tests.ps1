# Run Unity tests and reject empty or incomplete result files.
[CmdletBinding()]
param(
    [string]$UnityExe = 'E:\Unity\Unity\2022.3.62f3\Editor\Unity.exe',
    [string]$ProjectPath = '',
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
    [int]$MinimumTests = 1
)
$ErrorActionPreference = 'Stop'
if (-not $ProjectPath) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath $UnityExe)) { throw "Unity.exe not found: $UnityExe" }
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Close the Unity Editor before running tests.' }
$dir = Join-Path $ProjectPath 'Library/TestRun'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$xmlPath = Join-Path $dir ($Platform.ToLowerInvariant() + '.xml')
$logPath = Join-Path $dir ($Platform.ToLowerInvariant() + '.log')
if (Test-Path -LiteralPath $xmlPath) { Remove-Item -LiteralPath $xmlPath }
$arguments = @('-batchmode', '-projectPath', ('"' + $ProjectPath + '"'), '-runTests',
    '-testPlatform', $Platform, '-testResults', ('"' + $xmlPath + '"'), '-logFile', ('"' + $logPath + '"'))
if ($Platform -eq 'EditMode') { $arguments += '-nographics' }
$process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
Write-Host "Unity exit: $($process.ExitCode); results: $xmlPath"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if (-not (Test-Path -LiteralPath $xmlPath)) { throw 'Unity did not produce test results.' }
[xml]$results = Get-Content -LiteralPath $xmlPath -Encoding UTF8
$run = $results.'test-run'
Write-Host "total=$($run.total), passed=$($run.passed), failed=$($run.failed), skipped=$($run.skipped)"
if ([int]$run.total -lt $MinimumTests -or [int]$run.failed -ne 0 -or $run.result -ne 'Passed' -or [int]$run.passed -ne [int]$run.total) {
    throw 'Tests were empty, skipped, incomplete or failed. Inspect the XML and Unity log.'
}
exit 0
