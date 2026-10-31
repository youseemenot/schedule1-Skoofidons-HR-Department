using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
if(args.Length<3){Console.WriteLine("PairedMenuChecks HR.dll Minimap.dll GameDirectory [ThirdProvider.dll]");return 2;}
var search=new[]{Path.Combine(args[2],"MelonLoader","net6"),Path.Combine(args[2],"MelonLoader","Il2CppAssemblies")};
int phoneRequests=0,checks=0,failures=0;
AssemblyLoadContext.Default.Resolving+=(context,name)=>{
 if(name.Name.Contains("Phone")){phoneRequests++;return null;}
 var file=search.Select(p=>Path.Combine(p,name.Name+".dll")).FirstOrDefault(File.Exists);
 return file==null?null:context.LoadFromAssemblyPath(Path.GetFullPath(file));
};
void Expect(bool ok,string why){if(!ok)throw new Exception(why);}
void Check(string name,Action test){checks++;try{test();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+(e.InnerException??e));}}
var hr=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var map=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var calls=new Dictionary<string,int>();
foreach(var pair in new[]{(hr,"skoofidon.hr"),(map,"skoofidon.minimap")})
{
 var (assembly,id)=pair;
 Check(id+" has no Phone dependency and resolves all types",()=>{
  Expect(!assembly.GetReferencedAssemblies().Any(a=>a.Name.Contains("Phone")),"Hard phone reference");
  Expect(assembly.GetTypes().Length>10,"Types unresolved");
 });
 Check(id+" runtime panel and lifecycle methods JIT against actual interop",()=>{
  var types=assembly.GetTypes().Where(t=>t.Namespace?.StartsWith("Skoofidons.SharedSettings")==true||t.Name=="HrSettingsPanel"||t.Name=="MainMod"||t.Name=="Core");
  int count=0;foreach(var type in types)
   foreach(var method in type.GetMethods(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)))
    if(!method.ContainsGenericParameters&&method.GetMethodBody()!=null){RuntimeHelpers.PrepareMethod(method.MethodHandle);count++;}
  Console.WriteLine("Prepared "+count+" methods in "+id);
 });
 Check(id+" publishes independent CLR endpoint with viewport and footer",()=>{
  var endpoint=assembly.GetType("Skoofidons.SharedSettings.EntryPointV1");calls[id]=0;
  endpoint.GetMethod("Initialize").Invoke(null,new object[]{id,id,new Version(1,0), (Action)(()=>calls[id]++),(Action)(()=>{}),(Action)(()=>{})});
  endpoint.GetMethod("ConfigurePanel").Invoke(null,new object[]{(Action<float,float,string>)((w,h,l)=>{Expect(w==700&&h==500&&l=="ru","Lost viewport or locale");calls[id]+=10;}),(Func<string>)(()=>"status"),null,null});
 });
}
IEnumerable<object> Discover(Assembly owner,Assembly[] participants)=>(owner.GetType("Skoofidons.SharedSettings.V1.ProviderDiscovery").GetMethod("Discover").Invoke(null,new object[]{participants,null}) as IEnumerable).Cast<object>();
string Id(object p)=>(string)p.GetType().GetProperty("Id").GetValue(p);
foreach(var owner in new[]{hr,map})
{
 Check(owner.GetName().Name+" sees both panels and elects the same host",()=>{
  var list=Discover(owner,new[]{map,hr}).ToArray();Expect(list.Length==2,"Wrong provider count");
  var typed=Array.CreateInstance(list[0].GetType(),list.Length);for(int i=0;i<list.Length;i++)typed.SetValue(list[i],i);
  var winner=owner.GetType("Skoofidons.SharedSettings.V1.HostElection").GetMethod("Elect").Invoke(null,new object[]{typed});Expect(Id(winner)=="skoofidon.hr","Different host");
  foreach(var p in list){((Action<float,float,string>)p.GetType().GetProperty("Viewport").GetValue(p))(700,500,"ru");((Action)p.GetType().GetProperty("Draw").GetValue(p))();}
 });
 Check(owner.GetName().Name+" works standalone",()=>Expect(Discover(owner,new[]{owner}).Count()==1,"Standalone missing"));
}
Check("callbacks ran in their own independent DLL",()=>Expect(calls.Values.All(v=>v==22),"Callback identity collapsed"));
if(args.Length>3)
{
 var third=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[3]));
 Check("third provider appears in both unchanged product DLLs",()=>{
  foreach(var owner in new[]{hr,map})Expect(Discover(owner,new[]{hr,map,third}).Count()==3,"Third mod missing");
 });
}
Check("all 31 HR preferences are represented by actual panel bindings",()=>{
 var config=hr.GetType("SkoofidonsHRDepartment.Config");var panel=hr.GetType("SkoofidonsHRDepartment.HrSettingsPanel");
 var fields=config.GetFields(BindingFlags.Static|BindingFlags.Public).Where(f=>f.FieldType.IsGenericType&&f.FieldType.Name.StartsWith("MelonPreferences_Entry")).ToArray();
 var il=panel.GetMethod("CreateSettings").GetMethodBody().GetILAsByteArray();
 Expect(fields.Length==31,"Preference count changed");
 foreach(var field in fields)
 {
  var token=BitConverter.GetBytes(field.MetadataToken);bool found=false;
  for(int i=0;i<il.Length-4;i++)if(il[i]==0x7e&&il.Skip(i+1).Take(4).SequenceEqual(token)){found=true;break;}
  Expect(found,"Missing UI binding "+field.Name);
 }
});
Check("F3 is owned by shared host while F4 diagnostic remains",()=>{
 var main=map.GetType("MinimapMod.MainMod");var il=main.GetMethod("OnUpdate").GetMethodBody().GetILAsByteArray();
 // Detect Input.GetKeyDown arguments in decoded opcode stream, never byte-pattern-search operands.
 var one=new Dictionary<byte,OpCode>();var two=new Dictionary<byte,OpCode>();
 foreach(var f in typeof(OpCodes).GetFields()){if(f.GetValue(null) is OpCode op){if(op.Size==1)one[(byte)op.Value]=op;else two[(byte)op.Value]=op;}}
 var ints=new List<int>();
 for(int i=0;i<il.Length;){var op=il[i++]==0xfe?two[il[i++]]:one[il[i-1]];
  if(op==OpCodes.Ldc_I4)ints.Add(BitConverter.ToInt32(il,i));
  i+=op.OperandType switch{OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(il,i),_=>4};
 }
 Expect(!ints.Contains(284)&&ints.Contains(285),"Legacy F3 remains or F4 lost");
});
Check("neither product tried to resolve absent Phone App",()=>Expect(phoneRequests==0,"Phone requested"));
Console.WriteLine($"{checks-failures}/{checks} actual-pair checks passed");return failures==0?0:1;
