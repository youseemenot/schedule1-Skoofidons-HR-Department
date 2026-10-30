[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [string]$InteropDirectory,
    [string]$PackageCache,
    [string]$RestoreSource
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).Path
if (!$InteropDirectory) { $InteropDirectory = Join-Path $gameRoot 'MelonLoader\Il2CppAssemblies' }
$interopRoot = (Resolve-Path -LiteralPath $InteropDirectory).Path
$gameHash = (Get-FileHash -LiteralPath (Join-Path $gameRoot 'GameAssembly.dll') -Algorithm SHA512).Hash
$stampPath = Join-Path $interopRoot 'game-reference.json'
if (Test-Path -LiteralPath $stampPath) {
    $referenceHash = (Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json).GameAssemblySha512
} else {
    $installedInterop = [IO.Path]::GetFullPath((Join-Path $gameRoot 'MelonLoader\Il2CppAssemblies'))
    if ($interopRoot.TrimEnd('\') -ne $installedInterop.TrimEnd('\')) {
        throw 'External interop directory needs game-reference.json with GameAssemblySha512 from the generation input.'
    }
    $generatorConfig = Get-Content -LiteralPath (Join-Path $gameRoot 'MelonLoader\Dependencies\Il2CppAssemblyGenerator\Config.cfg') -Raw
    $hashMatch = [regex]::Match($generatorConfig, 'GameAssemblyHash\s*=\s*"([A-Fa-f0-9]+)"')
    $referenceHash = $hashMatch.Groups[1].Value
}
if ($referenceHash -ne $gameHash) {
    throw 'IL2CPP references do not match GameAssembly.dll. Start the updated game once to regenerate them, or supply freshly generated references with a matching stamp.'
}
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnetCommand) { $dotnetCommand.Source } else { Join-Path $env:ProgramFiles 'dotnet\dotnet.exe' }
$buildArgs = @('build', (Join-Path $projectRoot 'SkoofidonsHRDepartment.csproj'), '-c', 'Release', '--nologo',
    "-p:GameDirectory=$gameRoot", "-p:InteropDirectory=$interopRoot")
if ($PackageCache) { $buildArgs += "-p:RestorePackagesPath=$PackageCache" }
if ($RestoreSource) { $buildArgs += @('--source', $RestoreSource) }
& $dotnetPath @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }
$output = Join-Path $projectRoot 'bin\Release\net6.0\SkoofidonsHRDepartment.dll'
$assembly = [Reflection.AssemblyName]::GetAssemblyName($output)
$manifest = [ordered]@{
    ModVersion = $assembly.Version.ToString()
    GameAssemblySha512 = $gameHash
    ModSha256 = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    BuiltAtUtc = [DateTime]::UtcNow.ToString('o')
    RuntimeTesting = 'Pending singleplayer gameplay checks'
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path $output) 'build-info.json') -Encoding UTF8
Write-Output "Singleplayer test DLL: $output"
