[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GameDirectory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $GameDirectory).Path
Add-Type -Path "$root\MelonLoader\net6\Mono.Cecil.dll"
Add-Type -Path "$root\MelonLoader\net6\Iced.dll"
function AllTypes($ts) { foreach($t in $ts) { $t; AllTypes $t.NestedTypes } }
function Addr($m) { $a=$m.CustomAttributes | Where-Object {$_.AttributeType.Name -eq 'AddressAttribute'}; if($a) { $d=@{}; foreach($f in $a.Fields) {$d[$f.Name]=[Convert]::ToInt64($f.Argument.Value,16)}; return $d } }
$index=@{}; $chosen=@(); $assemblies=0; $methods=0
$select=@{
 'MonoState'='^(PushToDefaultParent|PopFromDefaultParent|RemoveFromDefaultParent|OnExit|Awake|InitializeDefaultParent)$'
 'MonoStateMachine'='^(Push|Pop|Remove)$'
 'StatePropertiesTransitionHandler'='^Transition$'
 'SceneState'='^Update$'
 'PlayerCamera'='^(AddActiveUIElement|RemoveActiveUIElement|LockMouse|SetCanLook|Update|LateUpdate|get_ActiveUIElementCount)$'
 'PlayerMovement'='^(get_CanMove|Move)$'
 'GameInput'='^(GetButton|GetButtonDown|GetButtonUp|OnInteract|Exit|RegisterExitListener|DeregisterExitListener)$'
 'UIScreenManager'='^(AddScreen|RemoveScreen|BackToCloseCurrentScreen)$'
 'PauseMenu'='^(Pause|Resume)$'
}
foreach($file in Get-ChildItem -LiteralPath "$root\MelonLoader\Dependencies\Il2CppAssemblyGenerator\Cpp2IL\cpp2il_out" -Filter 'Assembly-CSharp.dll') {
 $a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($file.FullName)
 try {
  $assemblies++
  foreach($t in AllTypes $a.MainModule.Types) {foreach($m in $t.Methods) {
   $methods++; $n=Addr $m; if(!$n) {continue}; $key=[long]$n.RVA
   if(!$index.ContainsKey($key)) {$index[$key]=[Collections.Generic.List[string]]::new()}
   $index[$key].Add($file.Name+': '+$m.FullName)
   if($select.ContainsKey($t.Name) -and $m.Name -match $select[$t.Name]) {$chosen+=@{FullName=$m.FullName; Rva=$n.RVA; Offset=$n.Offset; Length=$n.Length; Parameters=(($m.Parameters | ForEach-Object {$_.Name+': '+$_.ParameterType}) -join ', ')}}
  }}
 } finally {$a.Dispose()}
}
$s=[IO.File]::OpenRead("$root\GameAssembly.dll")
try {
 'SHA512 '+(Get-FileHash -LiteralPath "$root\GameAssembly.dll" -Algorithm SHA512).Hash
 foreach($m in $chosen) {
  'NATIVE '+$m.FullName+' RVA=0x{0:X} len={1}' -f $m.Rva,$m.Length
  'PARAMETERS '+$m.Parameters
  $b=[byte[]]::new($m.Length); $s.Position=$m.Offset; [void]$s.Read($b,0,$b.Length)
  $d=[Iced.Intel.Decoder]::Create(64,$b,[UInt64]$m.Rva,[Iced.Intel.DecoderOptions]::None)
  while($d.IP -lt $m.Rva+$m.Length) {
   $i=$d.Decode(); $label=''
   if($i.Mnemonic -in @([Iced.Intel.Mnemonic]::Call,[Iced.Intel.Mnemonic]::Jmp)) {$r=[long]$i.NearBranchTarget; if($index.ContainsKey($r)) {$label=if($index[$r].Count -le 6){' ; '+($index[$r] -join ' | ')}else{' ; SHARED ALIASES '+$index[$r].Count}}}
   ' {0:X}: {1}{2}' -f $i.IP,$i.ToString(),$label
  }
 }
}finally{$s.Dispose()}
