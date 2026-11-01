using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using HarmonyLib;
using UnityEngine;
if(args.Length<3){Console.WriteLine("LegacyMinimapChecks HR.dll legacyMinimap.dll GameDirectory");return 2;}
var directories=new[]{Path.Combine(args[2],"MelonLoader","net6"),Path.Combine(args[2],"MelonLoader","Il2CppAssemblies"),Path.Combine(args[2],"Mods")};
AssemblyLoadContext.Default.Resolving+=(c,n)=>{var p=directories.Select(d=>Path.Combine(d,n.Name+".dll")).FirstOrDefault(File.Exists);return p==null?null:c.LoadFromAssemblyPath(Path.GetFullPath(p));};
int checks=0,failures=0;
void Expect(bool ok,string why){if(!ok)throw new Exception(why);}
void Check(string name,Action action){checks++;try{action();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+(e.InnerException??e));}}
var hr=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var legacy=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var legacyType=legacy.GetType("MinimapMod.MainMod");var instance=FormatterServices.GetUninitializedObject(legacyType);
var before=SHA256.HashData(File.ReadAllBytes(args[1]));
Type Required(string name)=>hr.GetType("SkoofidonsHRDepartment."+name)??throw new Exception(name+" missing");
object Bind(){var m=Required("LegacyMinimapBinding").GetMethod("TryBind");var values=new object[]{instance,null,null};Expect((bool)m.Invoke(null,values),(string)values[2]);return values[1];}
if(args.Contains("--modern"))
{
 Check("modern Minimap is left to its native shared provider without patches",()=>{
  var values=new object[]{instance,null,null};Expect(!(bool)Required("LegacyMinimapBinding").GetMethod("TryBind").Invoke(null,values)&&((string)values[2]).Contains("already supports"),"Modern module was adapted");
  Expect(HarmonyLib.Harmony.GetPatchInfo(legacyType.GetMethod("OnUpdate"))?.Owners.Contains("skoofidon.hr.legacy-minimap")!=true,"Modern module patched");
 });
 Console.WriteLine($"{checks-failures}/{checks} modern-bypass checks passed");return failures==0?0:1;
}
Check("known original DLL binds all 23 existing setting fields",()=>{
 var b=Bind();var names=((IEnumerable<string>)b.GetType().GetProperty("Keys").GetValue(b)).ToArray();Expect(names.Length==23&&names.Distinct().Count()==23,"Incomplete binding");
 foreach(var name in names)Expect(legacyType.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!=null,"Missing original field "+name);
});
Check("managed fixture edits use validated original field slots",()=>{
 // Actual MainMod has a native Color static initializer; do not run it outside Unity.
 var fixture=new SettingsFixture();var type=Required("LegacyMinimapBinding");
 var b=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);
 var set=type.GetMethod("SetCached");set.MakeGenericMethod(typeof(float)).Invoke(b,new object[]{"mapOpacity",.7f});
 Expect(fixture.ReadOpacity==.7f,"Edited a copy instead of original slot");
 set.MakeGenericMethod(typeof(bool)).Invoke(b,new object[]{"useNavMode",true});Expect(fixture.ReadNavigation,"Toggle not bound");
 var apply=type.GetMethod("Set").MakeGenericMethod(typeof(float));apply.Invoke(b,new object[]{"mapOpacity",.6f});Expect(fixture.Saves==1&&fixture.ReadOpacity==.6f,"Original application callback not used");
});
Check("failed flush keeps pending data and bounds repeated error logging",()=>{
 var fixture=new SettingsFixture();fixture.QueueSave();var type=Required("LegacyMinimapBinding");
 var b=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);int warnings=0;
 type.GetMethod("Flush").Invoke(b,new object[]{50f,(Action<string>)(_=>warnings++)});
 Expect(fixture.Pending&&fixture.Due==55&&type.GetProperty("SaveError").GetValue(b)!=null,"Failed save was lost or hidden");
 type.GetMethod("Flush").Invoke(b,new object[]{60f,(Action<string>)(_=>warnings++)});
 Expect(fixture.Pending&&fixture.Due==65&&warnings==1,"Save failure spammed log or prevented retries");
});
Check("each tab resets original entry defaults only and queues one native application",()=>{
 var type=Required("LegacyMinimapBinding");var reset=type.GetMethod("ResetTab")??throw new Exception("Minimap current-tab reset missing");
 for(int tab=0;tab<4;tab++)
 {
  var fixture=new SettingsFixture();fixture.InitializePreferences();var before=fixture.PreferenceValues();int refreshes=fixture.Refreshes;
  var b=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);
  reset.Invoke(b,new object[]{tab,10f});
  foreach(var row in SettingsFixture.Rows)
   Expect(Equals(fixture.PreferenceValues()[row.Pref],row.Tab==tab?row.Default:before[row.Pref]),"Cross-tab preference reset: "+row.Pref);
  Expect(fixture.Refreshes==refreshes+1&&fixture.Pending&&fixture.Due==10.5f,"Missing/burst apply or changed debounce");
 }
});
Check("failed reset restores original values and original pending state",()=>{
 var type=Required("LegacyMinimapBinding");var reset=type.GetMethod("ResetTab")??throw new Exception("Minimap current-tab reset missing");
 var fixture=new SettingsFixture();fixture.InitializePreferences();var before=fixture.PreferenceValues();fixture.FailRefreshOnce=true;
 var b=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);
 try{reset.Invoke(b,new object[]{0,10f});throw new Exception("Failed application ignored");}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}
 Expect(before.All(p=>Equals(fixture.PreferenceValues()[p.Key],p.Value))&&!fixture.Pending&&fixture.Due==0,"Failed reset committed data or pending state");
});
Check("transpiler removes only F3 read preserving F4 and instruction identity",()=>{
 var input=typeof(FakeInput).GetMethod("GetKeyDown");var label=default(Label);
 var source=new[]{new CodeInstruction(OpCodes.Ldc_I4,284),new CodeInstruction(OpCodes.Call,input),new CodeInstruction(OpCodes.Pop),new CodeInstruction(OpCodes.Ldc_I4,285),new CodeInstruction(OpCodes.Call,input),new CodeInstruction(OpCodes.Pop),new CodeInstruction(OpCodes.Ret)};
 source[0].labels.Add(label);
 var result=((IEnumerable<CodeInstruction>)Required("LegacyMinimapPatches").GetMethod("Rewrite").Invoke(null,new object[]{source,input})).ToArray();
 Expect(result.Length==source.Length&&result[0].opcode==OpCodes.Ldc_I4&&result[1].opcode==OpCodes.Call&&((MethodInfo)result[1].operand).Name=="ReadLegacyKey","F3 branch not replaced");
 Expect(source[0].opcode==OpCodes.Ldc_I4&&source[1].opcode==OpCodes.Call,"Source modified before validation");
 Expect(result[0].labels.Count==1&&result.Skip(2).Zip(source.Skip(2)).All(p=>p.First.opcode==p.Second.opcode&&Equals(p.First.operand,p.Second.operand)),"Other instructions or labels changed");
});
Check("unexpected or duplicate F3 pattern is rejected before patching",()=>{
 var input=typeof(FakeInput).GetMethod("GetKeyDown");var rewrite=Required("LegacyMinimapPatches").GetMethod("Rewrite");
 foreach(var source in new[]{Array.Empty<CodeInstruction>(),new[]{new CodeInstruction(OpCodes.Ldc_I4,284),new CodeInstruction(OpCodes.Call,input),new CodeInstruction(OpCodes.Ldc_I4,284),new CodeInstruction(OpCodes.Call,input)}})
  try{((IEnumerable<CodeInstruction>)rewrite.Invoke(null,new object[]{source,input})).ToArray();throw new Exception("Bad method accepted");}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}
});
Check("actual supported OnUpdate IL passes the same exact rewrite",()=>{
 var source=PatchProcessor.GetOriginalInstructions(legacyType.GetMethod("OnUpdate"));
 var input=typeof(Input).GetMethod("GetKeyDown",new[]{typeof(KeyCode)});
 var result=((IEnumerable<CodeInstruction>)Required("LegacyMinimapPatches").GetMethod("Rewrite").Invoke(null,new object[]{source,input})).ToArray();Expect(result.Length==source.Count,"Unexpected instruction replacement");
});
Check("managed detour suppresses F3 and old GUI while F4 and map tick survive",()=>{
 var fixture=new SettingsFixture();var b=Activator.CreateInstance(Required("LegacyMinimapBinding"),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);
 Action cachedUpdate=fixture.OnUpdate,cachedGui=fixture.OnGUI;
 var type=Required("LegacyMinimapPatches");IDisposable handle=null;
 try
 {
  handle=(IDisposable)type.GetMethod("Install").Invoke(null,new[]{b});
  cachedUpdate();cachedGui();
  Expect(fixture.Ticks==1&&fixture.F4==1&&!fixture.Open&&fixture.GuiDraws==0,"Adapter changed map/F4 or left old window active");
  try{type.GetMethod("Install").Invoke(null,new[]{b});throw new Exception("Duplicate install accepted");}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}
 }
 finally{handle?.Dispose();handle?.Dispose();}
 fixture.OnGUI();Expect(fixture.GuiDraws==1,"Unpatch did not restore old GUI");
 Expect(HarmonyLib.Harmony.GetPatchInfo(typeof(SettingsFixture).GetMethod("OnUpdate"))?.Owners.Contains("skoofidon.hr.legacy-minimap")!=true,"Owned patch leaked");
});
Check("legacy panel and binding methods JIT against actual game references",()=>{
 int prepared=0;
 foreach(var type in hr.GetTypes().Where(t=>t.Name.StartsWith("LegacyMinimap")||t.DeclaringType?.Name.StartsWith("LegacyMinimap")==true))
  foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)))
   if(!method.ContainsGenericParameters&&method.GetMethodBody()!=null){RuntimeHelpers.PrepareMethod(method.MethodHandle);prepared++;}
 Expect(prepared>25,"Missing panel/adapter code");Console.WriteLine("Prepared "+prepared+" legacy adapter methods; no Unity callbacks executed");
});
Check("failed second patch rolls back first patch without affecting old methods",()=>{
 var b=Activator.CreateInstance(Required("LegacyMinimapBinding"),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{new SettingsFixture(),"fixture"},null);
 b.GetType().GetField("<GuiMethod>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,typeof(AbstractFixture).GetMethod("OnGUI"));
 try{((IDisposable)Required("LegacyMinimapPatches").GetMethod("Install").Invoke(null,new[]{b})).Dispose();throw new Exception("Abstract patch succeeded unexpectedly");}
 catch(TargetInvocationException){}
 Expect(HarmonyLib.Harmony.GetPatchInfo(typeof(SettingsFixture).GetMethod("OnUpdate"))?.Owners.Contains("skoofidon.hr.legacy-minimap")!=true,"Partial install left F3 patched");
});
Check("foreign patch owner is preserved and prevents partial adapter install",()=>{
 var fixture=new SettingsFixture();var b=Activator.CreateInstance(Required("LegacyMinimapBinding"),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{fixture,"fixture"},null);
 var other=new HarmonyLib.Harmony("legacy.fixture.other");
 try
 {
  other.Patch(typeof(SettingsFixture).GetMethod("OnGUI"),prefix:new HarmonyMethod(typeof(FakeInput).GetMethod("Prefix")));
  try{((IDisposable)Required("LegacyMinimapPatches").GetMethod("Install").Invoke(null,new[]{b})).Dispose();throw new Exception("Foreign owner ignored");}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}
  Expect(HarmonyLib.Harmony.GetPatchInfo(typeof(SettingsFixture).GetMethod("OnGUI")).Owners.Contains("legacy.fixture.other"),"Foreign patch removed");
  Expect(HarmonyLib.Harmony.GetPatchInfo(typeof(SettingsFixture).GetMethod("OnUpdate"))?.Owners.Contains("skoofidon.hr.legacy-minimap")!=true,"Partial update patch installed");
 }
 finally{other.UnpatchSelf();}
});
Check("same legacy assembly identity with an unknown digest is rejected",()=>{
 var path=Path.Combine(AppContext.BaseDirectory,"unknown-"+Guid.NewGuid()+".dll");var bytes=File.ReadAllBytes(args[1]);File.WriteAllBytes(path,bytes.Concat(new byte[]{0}).ToArray());
 var context=new AssemblyLoadContext("legacy-unknown",true);
 try
 {
  var modified=context.LoadFromAssemblyPath(path);var candidate=FormatterServices.GetUninitializedObject(modified.GetType("MinimapMod.MainMod"));var values=new object[]{candidate,null,null};
  Expect(!(bool)Required("LegacyMinimapBinding").GetMethod("TryBind").Invoke(null,values)&&((string)values[2]).Contains("Unsupported legacy"),"Unknown binary was patched");
 }
 finally{context.Unload();}
});
Check("HR still has no assembly reference to Minimap",()=>Expect(!hr.GetReferencedAssemblies().Any(a=>a.Name==legacy.GetName().Name),"Mandatory dependency introduced"));
Check("legacy DLL stays byte-for-byte unchanged",()=>Expect(before.SequenceEqual(SHA256.HashData(File.ReadAllBytes(args[1]))),"Original DLL rewritten"));
Console.WriteLine($"{checks-failures}/{checks} legacy-adapter checks passed");return failures==0?0:1;
public static class FakeInput { public static bool GetKeyDown(KeyCode key)=>key==KeyCode.F4;public static bool Prefix()=>true; }
public abstract class AbstractFixture { public abstract void OnGUI(); }
#pragma warning disable CS0649, CS0169
public sealed class SettingsFixture
{
 public static MelonLoader.MelonPreferences_Category prefCategory;
 public static MelonLoader.MelonPreferences_Entry<bool> prefEnabled,prefTime,prefNavMode,prefOrders,prefDealers,prefCustomers,prefDeadDrops,prefProperties,prefVehicles,prefCoop,prefPolice;
 public static MelonLoader.MelonPreferences_Entry<float> prefWindowSize,prefZoom,prefOpacity,prefMapOffsetX,prefMapOffsetY,prefTimeOffsetX,prefTimeOffsetY,prefTimeScale,prefArrowSize,prefArrowR,prefArrowG,prefArrowB;
 public static readonly (string Key,string Pref,int Tab,object Default)[] Rows={
  ("minimapEnabled","prefEnabled",0,true),("timeBarEnabled","prefTime",0,true),("useNavMode","prefNavMode",0,false),
  ("windowSizeMultiplier","prefWindowSize",0,1f),("mapZoom","prefZoom",0,1f),("mapOpacity","prefOpacity",0,1f),
  ("showOrdersEnabled","prefOrders",1,true),("showDealersEnabled","prefDealers",1,true),("showCustomersEnabled","prefCustomers",1,true),("showDeadDropsEnabled","prefDeadDrops",1,true),
  ("showPropertiesEnabled","prefProperties",1,true),("showVehiclesEnabled","prefVehicles",1,true),("coopRadarEnabled","prefCoop",1,true),("policeRadarEnabled","prefPolice",1,true),
  ("mapOffsetX","prefMapOffsetX",2,0f),("mapOffsetY","prefMapOffsetY",2,0f),("timeOffsetX","prefTimeOffsetX",2,0f),("timeOffsetY","prefTimeOffsetY",2,0f),("timeScale","prefTimeScale",2,1f),
  ("arrowSize","prefArrowSize",3,1.4f),("arrowR","prefArrowR",3,0f),("arrowG","prefArrowG",3,.8f),("arrowB","prefArrowB",3,1f)
 };
 public void InitializePreferences()
 {
  foreach(var row in Rows)
  {
   var field=typeof(SettingsFixture).GetField(row.Pref);var entry=(MelonLoader.MelonPreferences_Entry)Activator.CreateInstance(field.FieldType);
   field.FieldType.GetProperty("DefaultValue").SetValue(entry,row.Default);
   entry.BoxedValue=row.Default is bool value?!value:row.Pref.Contains("Offset")?123f:.5f;
   field.SetValue(null,entry);
  }
  HandleSettingsUpdate();
 }
 public Dictionary<string,object> PreferenceValues()=>Rows.ToDictionary(r=>r.Pref,r=>((MelonLoader.MelonPreferences_Entry)typeof(SettingsFixture).GetField(r.Pref).GetValue(null)).BoxedValue);
 private bool settingsSavePending,guiVisible;private float settingsSaveDueAt;
 private bool minimapEnabled,timeBarEnabled,showOrdersEnabled,showDealersEnabled,showCustomersEnabled,showDeadDropsEnabled,showPropertiesEnabled,showVehiclesEnabled,coopRadarEnabled,policeRadarEnabled;
 private float windowSizeMultiplier,mapZoom,mapOffsetX,mapOffsetY,timeOffsetX,timeOffsetY,timeScale,arrowSize,arrowR,arrowG,arrowB;
 // Field names match the real DLL; public access is avoided by the binding contract.
 private float mapOpacity;private bool useNavMode;
 public float ReadOpacity=>mapOpacity;public bool ReadNavigation=>useNavMode;public int Saves;
 public void QueueSave(){settingsSavePending=true;}public bool Pending=>settingsSavePending;public float Due=>settingsSaveDueAt;
 public int Ticks,F4,GuiDraws;public bool Open;
 [MethodImpl(MethodImplOptions.NoInlining)] public void OnUpdate(){Ticks++;if(Input.GetKeyDown(KeyCode.F3))Open=!Open;if(FakeInput.GetKeyDown(KeyCode.F4))F4++;}
 [MethodImpl(MethodImplOptions.NoInlining)] public void OnGUI(){GuiDraws++;}
 public int Refreshes;public bool FailRefreshOnce;
 private void HandleSettingsUpdate()
 {
  Refreshes++;if(prefEnabled!=null)foreach(var row in Rows)typeof(SettingsFixture).GetField(row.Key,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(this,((MelonLoader.MelonPreferences_Entry)typeof(SettingsFixture).GetField(row.Pref).GetValue(null)).BoxedValue);
  if(FailRefreshOnce){FailRefreshOnce=false;throw new InvalidOperationException("Native apply failed");}
 }
 private void SaveSettingsFromF3(){Saves++;}private void FlushPendingSettings(){}
}
