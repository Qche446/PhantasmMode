[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',
    [string] $TmodOutputPath = ''
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'Monochrome.csproj'
$buildProperties = @()
if (-not [string]::IsNullOrWhiteSpace($TmodOutputPath)) {
    $buildProperties += ('-p:TmodOutputPath=' + [IO.Path]::GetFullPath($TmodOutputPath))
}

Write-Host 'Using the tml-build stable release (independent of the Steam branch).'
# A separate restore lets the build read freshly generated aliases and NuGet targets.
& dotnet restore $projectFile --disable-parallel -m:1 --nologo @buildProperties
if ($LASTEXITCODE -ne 0) {
    throw "Monochrome restore failed (exit $LASTEXITCODE)."
}
& dotnet build $projectFile -c $Configuration --no-restore --nologo @buildProperties
if ($LASTEXITCODE -ne 0) {
    throw "Monochrome build failed (exit $LASTEXITCODE)."
}
