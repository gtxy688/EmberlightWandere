# Export an importable Unity project including actual assets and all source files.
[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$OutputPath = 'Delivery/EmberlightWandere-project.zip',
    [switch]$Force
)
$ErrorActionPreference = 'Stop'
if (-not $ProjectPath) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$output = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $ProjectPath $OutputPath }
if ((Test-Path -LiteralPath $output) -and -not $Force) { throw 'Output already exists. Use -Force to replace the previous export.' }
$folders = @('Assets', 'Packages', 'ProjectSettings', 'Tools', 'Docs')
$files = @()
foreach ($folder in $folders) {
    $path = Join-Path $ProjectPath $folder
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Missing project folder: $folder" }
    $files += Get-ChildItem -LiteralPath $path -Recurse -File
}
foreach ($name in @('README.md', 'AGENTS.md', '.gitignore', '.gitattributes')) {
    $path = Join-Path $ProjectPath $name
    if (Test-Path -LiteralPath $path) { $files += Get-Item -LiteralPath $path }
}
foreach ($file in $files) {
    $stream = [IO.File]::OpenRead($file.FullName)
    try {
        $buffer = New-Object byte[] 128
        $count = $stream.Read($buffer, 0, $buffer.Length)
        if ([Text.Encoding]::ASCII.GetString($buffer, 0, $count).StartsWith('version https://git-lfs.github.com/spec/v1')) {
            throw "Git LFS pointer instead of actual resource: $($file.FullName)"
        }
    } finally { $stream.Dispose() }
    if ($file.FullName.StartsWith((Join-Path $ProjectPath 'Assets') + '\') -and $file.Extension -ne '.meta' -and
        -not (Test-Path -LiteralPath ($file.FullName + '.meta'))) { throw "Missing .meta: $($file.FullName)" }
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$temporary = $output + '.tmp'
if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
$archive = [IO.Compression.ZipFile]::Open($temporary, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($ProjectPath.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, 'EmberlightWandere/' + $relative) | Out-Null
    }
    foreach ($name in @('editmode.xml', 'playmode.xml', 'clean-import-editmode.xml')) {
        $path = Join-Path $ProjectPath ('Library/TestRun/' + $name)
        if (Test-Path -LiteralPath $path) {
            [xml]$results = Get-Content -LiteralPath $path -Encoding UTF8
            $run = $results.'test-run'
            if ($run.result -eq 'Passed' -and [int]$run.total -gt 0 -and [int]$run.passed -eq [int]$run.total) {
                [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $path, 'EmberlightWandere/Verification/' + $name) | Out-Null
            } else { throw "Refusing to include failed/incomplete test evidence: $name" }
        }
    }
} finally { $archive.Dispose() }
Move-Item -LiteralPath $temporary -Destination $output -Force
$hash = Get-FileHash -LiteralPath $output -Algorithm SHA256
Write-Host "Exported $($files.Count) files -> $output"
Write-Host "SHA256: $($hash.Hash)"
exit 0
