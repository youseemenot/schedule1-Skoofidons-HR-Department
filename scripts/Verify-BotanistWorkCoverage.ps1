[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$GameDirectory)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).Path
Add-Type -Path (Join-Path $gameRoot 'MelonLoader\net6\Mono.Cecil.dll')
Add-Type -Path (Join-Path $gameRoot 'MelonLoader\net6\Iced.dll')
$metadataRoot = Join-Path $gameRoot 'MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\cpp2il_out'
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $metadataRoot 'Assembly-CSharp.dll'))
$unity = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $metadataRoot 'UnityEngine.CoreModule.dll'))
$stream = [IO.File]::OpenRead((Join-Path $gameRoot 'GameAssembly.dll'))
$reader = [IO.BinaryReader]::new($stream)
$script:passed = 0; $script:failed = 0
function Assert([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Check([string]$checkLabel, [scriptblock]$test) {
    try { & $test; $script:passed++; "PASS $checkLabel" }
    catch { $script:failed++; "FAIL ${checkLabel}: $($_.Exception.Message)" }
}
function Native-Info($method) {
    $address = $method.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'AddressAttribute' }
    if (!$address) { throw "Missing native metadata: $($method.FullName)" }
    $info = @{}
    foreach ($field in $address.Fields) { $info[$field.Name] = [Convert]::ToInt64($field.Argument.Value,16) }
    return $info
}
function Game-Type([string]$name) {
    $type = $assembly.MainModule.Types | Where-Object FullName -eq $name
    if (!$type) { throw "Missing type: $name" }
    return $type
}
function Routine([string]$owner, [string]$action) {
    $type = Game-Type "ScheduleOne.NPCs.Behaviour.$owner"
    $nested = @($type.NestedTypes | Where-Object Name -like "*<$action>*")
    Assert ($nested.Count -eq 1) "Ambiguous coroutine: $owner.$action"
    return $nested[0].Methods | Where-Object Name -eq 'MoveNext'
}
function Instructions($method) {
    $info = Native-Info $method
    $bytes = New-Object byte[] $info.Length
    $stream.Position = $info.Offset; [void]$stream.Read($bytes,0,$bytes.Length)
    $decoder = [Iced.Intel.Decoder]::Create(64,$bytes,[UInt64]$info.RVA,[Iced.Intel.DecoderOptions]::None)
    while ($decoder.IP -lt $info.RVA+$info.Length) { $decoder.Decode() }
}
function Speed-Calls($instructions) {
    return @($instructions | Where-Object {
        $_.Mnemonic -eq [Iced.Intel.Mnemonic]::Call -and $_.NearBranchTarget -eq $workSpeedRva
    }).Count
}
function Fixed-Waits($instructions) {
    $values = @()
    for ($index=0; $index -lt $instructions.Count; $index++) {
        $instruction = $instructions[$index]
        if ($instruction.Mnemonic -ne [Iced.Intel.Mnemonic]::Call -or $instruction.NearBranchTarget -ne $waitCtorRva) { continue }
        for ($prior=$index-1; $prior -ge [Math]::Max(0,$index-8); $prior--) {
            $load = $instructions[$prior]
            if ($load.Mnemonic -eq [Iced.Intel.Mnemonic]::Movss -and $load.Op0Register -eq [Iced.Intel.Register]::XMM1 -and $load.IsIPRelativeMemoryOperand) {
                $rva = [long]$load.IPRelativeMemoryAddress
                $section = $sections | Where-Object { $rva -ge $_.Rva -and $rva -lt $_.Rva+$_.Size } | Select-Object -First 1
                if (!$section) { throw 'Float constant outside PE sections' }
                $stream.Position = $section.Raw+$rva-$section.Rva
                $values += $reader.ReadSingle()
                break
            }
        }
    }
    return $values
}
try {
    $expectedHash = '872DFB646C9B866C53DAE12D7A34E1F9DF21A3E16FAC6B7C6CD21BF5FC8E4B3F99932163C5DE7E3798D37641712E82B914D46E1F023D56E77970BE84FB74EC3A'
    $actualHash = (Get-FileHash -LiteralPath (Join-Path $gameRoot 'GameAssembly.dll') -Algorithm SHA512).Hash
    # Disassembly assertions are intentionally restricted to the audited binary.
    if ($actualHash -ne $expectedHash) { throw 'Unsupported game binary: redo the native coverage audit before using these assertions.' }
    $stream.Position=0x3c; $peOffset=$reader.ReadInt32()
    $stream.Position=$peOffset+6; $sectionCount=$reader.ReadUInt16()
    $stream.Position=$peOffset+20; $optionalLength=$reader.ReadUInt16()
    $sections=@()
    for ($i=0; $i -lt $sectionCount; $i++) {
        $stream.Position=$peOffset+24+$optionalLength+40*$i+8
        $virtualSize=$reader.ReadUInt32(); $rva=$reader.ReadUInt32(); $rawSize=$reader.ReadUInt32(); $raw=$reader.ReadUInt32()
        $sections += @{Rva=$rva; Size=[Math]::Max($virtualSize,$rawSize); Raw=$raw}
    }
    $employee = Game-Type 'ScheduleOne.Employees.Employee'
    $workSpeed = $employee.Methods | Where-Object Name -eq 'get_CurrentWorkSpeed'
    $workSpeedRva = (Native-Info $workSpeed).RVA
    $wait = $unity.MainModule.Types | Where-Object FullName -eq 'UnityEngine.WaitForSeconds'
    $waitCtorRva = (Native-Info ($wait.Methods | Where-Object Name -eq '.ctor')).RVA
    Check 'native work getter and wait constructor match the audit' {
        Assert ($workSpeedRva -eq 0x6D7C80 -and $waitCtorRva -eq 0x5C4850) 'Native entry points changed'
    }
    foreach ($name in @('AddSoilToGrowContainerBehaviour','WaterPotBehaviour','ApplyAdditiveToGrowContainerBehaviour','SowSeedInPotBehaviour','HarvestPotBehaviour','ApplySpawnToMushroomBedBehaviour','HarvestMushroomBedBehaviour','MistMushroomBedBehaviour')) {
        Check "$name inherits the common work-speed action pipeline" {
            $type = Game-Type "ScheduleOne.NPCs.Behaviour.$name"
            while ($type -and $type.FullName -ne 'ScheduleOne.NPCs.Behaviour.GrowContainerBehaviour') {
                $parent = $type.BaseType.FullName
                $type = $assembly.MainModule.Types | Where-Object FullName -eq $parent
            }
            Assert ($null -ne $type) 'Action no longer inherits GrowContainerBehaviour'
        }
    }
    Check 'common pot and fungal action divides by CurrentWorkSpeed once' {
        $code = @(Instructions (Routine 'GrowContainerBehaviour' 'PerformAction'))
        Assert ((Speed-Calls $code) -eq 1) 'Unexpected work-speed read count'
        Assert (@($code | Where-Object Mnemonic -eq ([Iced.Intel.Mnemonic]::Divss)).Count -eq 1) 'Unexpected duration division'
    }
    foreach ($entry in @(@('StartDryingRackBehaviour','BeginAction'),@('UseSpawnStationBehaviour','BeginWork'))) {
        Check "$($entry[0]) scales native work time" {
            $code = @(Instructions (Routine $entry[0] $entry[1]))
            Assert ((Speed-Calls $code) -eq 1) 'Work-speed read missing'
            Assert (@($code | Where-Object Mnemonic -eq ([Iced.Intel.Mnemonic]::Divss)).Count -eq 1) 'Native work-time division missing'
        }
    }
    Check 'dryer unload keeps its single fixed animation wait' {
        $code = @(Instructions (Routine 'StopDryingRackBehaviour' 'BeginAction'))
        $waits = @(Fixed-Waits $code)
        Assert ((Speed-Calls $code) -eq 0 -and $waits.Count -eq 1 -and $waits[0] -eq 0.5) 'Unload timing differs from preserved baseline'
    }
    Check 'supply pickup keeps both fixed half-second waits' {
        $code = @(Instructions (Routine 'GrowContainerBehaviour' 'GrabRequiredItemFromSupplies'))
        $waits = @(Fixed-Waits $code)
        Assert ((Speed-Calls $code) -eq 0 -and $waits.Count -eq 2 -and $waits[0] -eq 0.5 -and $waits[1] -eq 0.5) 'Supply pickup timing changed'
    }
    "Native coverage: $script:passed passed, $script:failed failed. No Unity or employee callbacks were executed."
    if ($script:failed -ne 0) { throw 'Native coverage checks failed' }
} finally { $assembly.Dispose(); $unity.Dispose(); $reader.Dispose(); $stream.Dispose() }
