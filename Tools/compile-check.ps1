# Compile Runtime sources using the current Unity-generated response file.
# Import the project once in Unity first. This check does not compile Editor/tests.
[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$UnityExe = 'E:\Unity\Unity\2022.3.62f3\Editor\Unity.exe',
    [string]$OutDll = 'Library/RefactorCheck/Emberlight.Runtime.dll'
)
$ErrorActionPreference = 'Stop'
if (-not $ProjectPath) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if ($env:UNITY_EDITOR) {
    $UnityExe = if (Test-Path $env:UNITY_EDITOR -PathType Container) {
        Join-Path $env:UNITY_EDITOR 'Unity.exe'
    } else { $env:UNITY_EDITOR }
}
$csc = Join-Path (Split-Path -Parent $UnityExe) 'Data/DotNetSdkRoslyn/csc.dll'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet -or -not (Test-Path -LiteralPath $csc)) {
    Write-Host 'Requires dotnet and the project Unity Editor. Pass -UnityExe or set UNITY_EDITOR.'
    exit 2
}
$rsp = Get-ChildItem (Join-Path $ProjectPath 'Library/Bee/artifacts') -Recurse -Filter Emberlight.Runtime.rsp -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if (-not $rsp) {
    Write-Host 'No Emberlight.Runtime.rsp. Open/import the project in Unity first.'
    exit 2
}
$outFull = if ([IO.Path]::IsPathRooted($OutDll)) { $OutDll } else { Join-Path $ProjectPath $OutDll }
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outFull) | Out-Null
$checkDir = Join-Path $ProjectPath 'Library/RefactorCheck'
New-Item -ItemType Directory -Force -Path $checkDir | Out-Null
$checkRsp = Join-Path $checkDir 'compile.rsp'
$options = @(Get-Content -LiteralPath $rsp.FullName -Encoding UTF8 | Where-Object {
    # Never overwrite Bee outputs/ref assemblies: their identity is used by Unity.
    $_ -notmatch '^[-/](out|refout):' -and $_ -notmatch '^"?.+\.cs"?$'
})
$sources = @(Get-ChildItem (Join-Path $ProjectPath 'Assets/Scripts') -Recurse -Filter *.cs |
    ForEach-Object { '"' + $_.FullName + '"' })
[IO.File]::WriteAllLines($checkRsp, [string[]]($options + $sources + ('-out:"' + $outFull + '"')), [Text.UTF8Encoding]::new($false))
Write-Host "Response: $($rsp.FullName); sources: $($sources.Count)"
Push-Location $ProjectPath
try {
    $output = & $dotnet.Source $csc "@$checkRsp" 2>&1
    $code = $LASTEXITCODE
} finally { Pop-Location }
$output | ForEach-Object { Write-Host "$_" }
if ($code -eq 0) { Write-Host "COMPILE OK -> $outFull" }
else { Write-Host "COMPILE FAILED (exit $code)" }
exit $code