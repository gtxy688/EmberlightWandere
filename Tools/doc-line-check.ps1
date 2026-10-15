# Verify that `File.cs:NN` citations in Docs/ still point at the named identifier.
# Line numbers decay silently when code is reformatted, so the convention is:
#   `Assets/Scripts/Core/EmberGame.cs:101` + (backtick-quoted identifier) after it.
# The script checks the named identifier still appears on (or within a few lines of)
# the cited line. Use -Fix to rewrite the line number to where the identifier now is.
#
# NOTE: keep this file ASCII-only, using backtick escapes such as `u{FF08} where a
# full-width bracket is needed. Windows PowerShell 5.1 reads a BOM-less script as
# ANSI, so raw non-ASCII bytes here become mojibake (see AGENTS.md hard constraint 11).
[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$DocsPath = 'Docs',
    [int]$Tolerance = 3,
    [switch]$Fix
)
$ErrorActionPreference = 'Stop'
if (-not $ProjectPath) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$docsRoot = Join-Path $ProjectPath $DocsPath
if (-not (Test-Path -LiteralPath $docsRoot)) { Write-Host "No $DocsPath directory"; exit 2 }

$lparen = [char]0xFF08
$rparen = [char]0xFF09
# `path.cs:NN` or `path.cs:NN-MM`, optionally followed by an identifier label in
# either half-width or full-width brackets.
$pattern = '`(?<file>[A-Za-z0-9_/\.\- ]+\.cs):(?<line>\d+)(?:[-\u2013](?<end>\d+))?`' +
           '(?:\s*[\(\uFF08]\s*`(?<ident>[^`]+)`\s*[\)\uFF09])?'
$citeRe = [regex]$pattern

function Resolve-SourceFile([string]$rel) {
    $leaf = Split-Path $rel -Leaf
    $norm = $rel.Replace('\', '/')
    $hits = Get-ChildItem (Join-Path $ProjectPath 'Assets') -Recurse -Filter $leaf -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName.Replace('\', '/') -like "*$norm" }
    return ($hits | Select-Object -First 1)
}

function Find-Identifier([string[]]$lines, [string]$ident, [int]$from, [int]$tol) {
    # Nearest exact occurrence of the identifier token.
    $best = -1; $bestDist = [int]::MaxValue
    $token = [regex]::Escape($ident)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch $token) { continue }
        $dist = [Math]::Abs($i + 1 - $from)
        if ($dist -lt $bestDist) { $bestDist = $dist; $best = $i + 1 }
    }
    if ($best -lt 0) { return -1 }
    if ($bestDist -gt $tol) { return -2 }
    return $best
}

$checked = 0; $unlabelled = 0; $ok = 0; $drift = @(); $broken = @(); $fixed = 0

foreach ($doc in (Get-ChildItem $docsRoot -Recurse -Filter *.md)) {
    $text = [System.IO.File]::ReadAllText($doc.FullName, [System.Text.UTF8Encoding]::new($false))
    $rewrite = $text
    foreach ($m in $citeRe.Matches($text)) {
        $rel = $m.Groups['file'].Value.Trim()
        $line = [int]$m.Groups['line'].Value
        $checked++
        $src = Resolve-SourceFile $rel
        if (-not $src) {
            $broken += [pscustomobject]@{ Doc = $doc.Name; Cite = $m.Value; Why = 'source file not found' }
            continue
        }
        $lines = [System.IO.File]::ReadAllLines($src.FullName, [System.Text.UTF8Encoding]::new($false))
        if ($line -lt 1 -or $line -gt $lines.Count) {
            $broken += [pscustomobject]@{ Doc = $doc.Name; Cite = $m.Value; Why = "line out of range (file has $($lines.Count) lines)" }
            continue
        }
        if (-not $m.Groups['ident'].Success) { $unlabelled++; continue }
        $ident = $m.Groups['ident'].Value
        if ($lines[$line - 1] -match [regex]::Escape($ident)) { $ok++; continue }
        $found = Find-Identifier $lines $ident $line $Tolerance
        if ($found -eq -1) {
            $broken += [pscustomobject]@{ Doc = $doc.Name; Cite = $m.Value; Why = "identifier '$ident' not present in file" }
            continue
        }
        if ($found -eq -2) {
            $broken += [pscustomobject]@{ Doc = $doc.Name; Cite = $m.Value; Why = "identifier '$ident' not within +/-$Tolerance lines" }
            continue
        }
        $drift += [pscustomobject]@{ Doc = $doc.Name; Cite = $m.Value; Should = $found; Text = $lines[$found - 1].Trim() }
        if ($Fix) {
            $old = "``$($m.Groups['file'].Value):$line``"
            $new = "``$($m.Groups['file'].Value):$found``"
            if ($rewrite.Contains($old)) { $rewrite = $rewrite.Replace($old, $new); $fixed++ }
        }
    }
    if ($Fix -and $rewrite -ne $text) {
        [System.IO.File]::WriteAllText($doc.FullName, $rewrite, [System.Text.UTF8Encoding]::new($false))
    }
}

Write-Host "citations total   : $checked"
Write-Host "labelled and ok   : $ok"
Write-Host "unlabelled        : $unlabelled  (cannot be machine-checked; add the identifier label)"
if ($drift.Count) {
    Write-Host ''
    Write-Host 'drifted line numbers:'
    foreach ($d in $drift) {
        Write-Host ("  {0}" -f $d.Doc)
        Write-Host ("    {0} -> should be :{1}   {2}" -f $d.Cite, $d.Should, $d.Text)
    }
}
if ($broken.Count) {
    Write-Host ''
    Write-Host 'needs manual attention:'
    foreach ($b in $broken) { Write-Host ("  {0}  {1}  {2}" -f $b.Doc, $b.Cite, $b.Why) }
}
if ($Fix) { Write-Host ''; Write-Host "line numbers rewritten: $fixed" }

if ($broken.Count -gt 0) { exit 1 }
if ($drift.Count -gt 0 -and -not $Fix) { exit 1 }
exit 0
