[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [string]$PackageCache,
    [string]$RestoreSource
)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).Path
$hash = (Get-FileHash -LiteralPath (Join-Path $gameRoot 'GameAssembly.dll') -Algorithm SHA512).Hash
$generator = Get-Content -LiteralPath (Join-Path $gameRoot 'MelonLoader\Dependencies\Il2CppAssemblyGenerator\Config.cfg') -Raw
$referenceHash = [regex]::Match($generator, 'GameAssemblyHash\s*=\s*"([A-Fa-f0-9]+)"').Groups[1].Value
if ($referenceHash -ne $hash) { throw 'Installed IL2CPP references do not match GameAssembly.dll.' }
$protocol = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ProbeProtocol.cs') -Raw
$expected = [regex]::Match($protocol, 'ExpectedGameSha512\s*=\s*"([A-Fa-f0-9]+)"').Groups[1].Value
if ($hash -ne $expected) { throw 'Probe is bound to an earlier audited game binary. Re-audit before building.' }
$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
$buildArgs = @('build', (Join-Path $PSScriptRoot 'BotanistTimingProbe.csproj'), '-c', 'Release', '--nologo', "-p:GameDirectory=$gameRoot")
if ($PackageCache) { $buildArgs += "-p:RestorePackagesPath=$PackageCache" }
if ($RestoreSource) { $buildArgs += @('--source',$RestoreSource) }
& $dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
$output = Join-Path $PSScriptRoot 'bin\Release\net6.0\SkoofidonsHRBotanistProbe.dll'
[ordered]@{ProbeVersion='0.1.0';GameAssemblySha512=$hash;ProbeSha256=(Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash;Mode='Observation only, one contextual coroutine hook';RuntimeDispatch='Pending in-game test';BuiltAtUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path $output) 'probe-info.json') -Encoding utf8
Write-Output "Diagnostic DLL: $output"
