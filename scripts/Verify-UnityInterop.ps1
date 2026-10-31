[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ModPath,[Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $GameDirectory 'MelonLoader\net6\Mono.Cecil.dll')
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory((Join-Path $GameDirectory 'MelonLoader\Il2CppAssemblies'))
$resolver.AddSearchDirectory((Join-Path $GameDirectory 'MelonLoader\net6'))
$reader=[Mono.Cecil.ReaderParameters]::new();$reader.AssemblyResolver=$resolver
$mod=[Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $ModPath).Path,$reader)
$seen=[Collections.Generic.HashSet[string]]::new()
$failures=[Collections.Generic.List[string]]::new()
$queue=[Collections.Generic.Queue[object]]::new()
foreach($member in $mod.MainModule.GetMemberReferences()) {
    if($member -is [Mono.Cecil.MethodReference] -and $member.DeclaringType.Namespace.StartsWith('UnityEngine')) {$queue.Enqueue($member)}
}
try {
    while($queue.Count) {
        $reference=$queue.Dequeue()
        if(!$seen.Add($reference.FullName)){continue}
        $method=$reference.Resolve()
        if(!$method){throw "Unresolved Unity method: $($reference.FullName)"}
        if(!$method.HasBody){continue}
        $unsupported=@($method.Body.Instructions | Where-Object {$_.OpCode.Name -eq 'ldstr' -and $_.Operand -eq 'Method unstripping failed'})
        if($unsupported.Count){$failures.Add($reference.FullName);continue}
        foreach($instruction in $method.Body.Instructions) {
            if($instruction.Operand -is [Mono.Cecil.MethodReference] -and $instruction.Operand.DeclaringType.Namespace.StartsWith('UnityEngine')) {
                $queue.Enqueue($instruction.Operand)
            }
        }
    }
    foreach($failure in $failures){Write-Output ('UNSUPPORTED UNITY STUB: '+$failure)}
    Write-Output "Unity interop body audit: $($seen.Count) reachable methods; $($failures.Count) unsupported stubs. Does not execute Unity."
    if($failures.Count){throw 'Referenced Unity methods contain unstripping-failure stubs.'}
} finally {$mod.Dispose();$resolver.Dispose()}
