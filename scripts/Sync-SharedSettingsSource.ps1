[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)][string]$MinimapRepository,
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$source = Join-Path (Split-Path -Parent $PSScriptRoot) 'SharedSettings/V1'
$repository = (Resolve-Path -LiteralPath $MinimapRepository).Path
if (!(Test-Path -LiteralPath (Join-Path $repository 'SniccupMiniMap.csproj'))) {
    throw 'Destination is not a Minimap checkout.'
}
$destination = [IO.Path]::GetFullPath((Join-Path $repository 'MinimapMod/SharedSettings/V1'))
$prefix = $repository.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
if (!$destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination escaped the specified repository.'
}
# Reject reparse points along the destination path rather than writing through a junction.
$cursor = $destination
while ($cursor.Length -ge $repository.Length) {
    if (Test-Path -LiteralPath $cursor) {
        if ((Get-Item -LiteralPath $cursor).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Destination contains a reparse point: $cursor"
        }
    }
    if ($cursor -eq $repository) { break }
    $cursor = Split-Path -Parent $cursor
}
$files = @(Get-ChildItem -LiteralPath $source -Filter '*.cs' -File)
if (!$files.Count) { throw 'No shared settings sources found.' }
$entry = [IO.File]::ReadAllText((Join-Path $source 'EntryPointV1.cs'))
$version = [regex]::Match($entry, 'public const string RuntimeVersion = "([0-9.]+)"').Groups[1].Value
if (!$version) { throw 'Runtime version is not declared by the V1 endpoint.' }
Write-Output "Shared settings runtime $version; $($files.Count) sources -> $destination"
if (!$Apply -or !$PSCmdlet.ShouldProcess($destination, 'Copy shared settings source files')) { return }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$manifest = foreach ($file in $files) {
    $target = Join-Path $destination $file.Name
    Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    $expected = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) {
        throw "Copied source differs: $($file.Name)"
    }
    [ordered]@{Name=$file.Name;Sha256=$expected}
}
[ordered]@{Contract='Skoofidons.SettingsMenu/1';RuntimeVersion=$version;Files=@($manifest)} |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $destination 'vendor-manifest.json') -Encoding utf8
