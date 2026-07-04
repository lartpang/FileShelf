param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Runtime) -or $Runtime -notmatch '^[A-Za-z0-9][A-Za-z0-9.-]*$' -or $Runtime.Contains("..")) {
    throw "Runtime must be a runtime identifier, not a path: $Runtime"
}

if (-not [string]::IsNullOrWhiteSpace($Version) -and $Version -notmatch '^[0-9]+(\.[0-9]+){1,3}([\-+][A-Za-z0-9.-]+)?$') {
    throw "Version must be numeric semantic version text, for example 1.2.3: $Version"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\FileShelf.Win\FileShelf.Win.csproj"
$outputPath = Join-Path $repoRoot "artifacts\FileShelf-portable-$Runtime"
$resolvedOutputPath = [System.IO.Path]::GetFullPath($outputPath)
$resolvedArtifactsPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts")).TrimEnd("\", "/")
$artifactsRoot = $resolvedArtifactsPath + [System.IO.Path]::DirectorySeparatorChar

if (-not $resolvedOutputPath.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean a path outside artifacts: $resolvedOutputPath"
}

if (Test-Path -LiteralPath $resolvedOutputPath) {
    Remove-Item -LiteralPath $resolvedOutputPath -Recurse -Force
}

$publishArgs = @(
    "publish",
    $projectPath,
    "-c",
    $Configuration,
    "-r",
    $Runtime,
    "--self-contained",
    "false",
    "--no-restore",
    "-o",
    $resolvedOutputPath
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $publishArgs += "/p:Version=$Version"
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$statePath = Join-Path $resolvedOutputPath "FileShelfData"
if (Test-Path -LiteralPath $statePath) {
    Remove-Item -LiteralPath $statePath -Recurse -Force
}

Get-ChildItem -LiteralPath $resolvedOutputPath -Filter "*.pdb" -File |
    Remove-Item -Force

Write-Host "Portable build ready: $resolvedOutputPath"
