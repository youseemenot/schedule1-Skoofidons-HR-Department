#if IMPLEMENTED
using SkoofidonsHRDepartment;
#endif
using Skoofidons.SharedSettings.V1;
int n=0,failures=0;
void Check(string name,Action test){n++;try{test();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Expect(bool ok,string why){if(!ok)throw new Exception(why);}
#if IMPLEMENTED
NumericSetting Setting(string id,Func<double> get,Action<double> set,bool integer=false,double min=0)=>new(id,0,"ru","en",get,set,integer,min);
Check("validation is atomic and rejects nonfinite/incomplete numbers",()=>{
 double a=1,b=2;var d=new HrSettingsDraft(new[]{Setting("a",()=>a,v=>a=v),Setting("b",()=>b,v=>b=v)});d.Read();
 foreach(var bad in new[]{"","-","NaN","Infinity","1,2.3"}){d.Text["a"]="7";d.Text["b"]=bad;Expect(!d.TryApply(()=>{},()=>{},out _)&&a==1&&b==2,"Partial invalid edit committed");}
});
Check("dot comma integer values and unlimited valid limits survive",()=>{
 double a=1,b=3;var d=new HrSettingsDraft(new[]{Setting("a",()=>a,v=>a=v,false,double.Epsilon),Setting("b",()=>b,v=>b=v,true,1)});d.Read();
 d.Text["a"]="0,5";d.Text["b"]="2000000000";Expect(d.TryApply(()=>{},()=>{},out _)&&a==.5&&b==2000000000,"Values capped or comma rejected");
 d.Text["b"]="3.5";Expect(!d.TryValidate(out _),"Fractional limit accepted");d.Text["b"]="2147483648";Expect(!d.TryValidate(out _),"Overflow accepted");
 d.Text["b"]="21";d.Text["a"]="0";Expect(!d.TryValidate(out _),"Zero speed accepted");
});
Check("write failure rolls preferences back and keeps draft",()=>{
 double a=1;int applied=0;var d=new HrSettingsDraft(new[]{Setting("a",()=>a,v=>a=v)});d.Read();d.Text["a"]="7";
 Expect(!d.TryApply(()=>throw new IOException("Read only"),()=>applied++,out _)&&a==1&&applied==0&&d.Dirty,"Failed persistence lost edits or changed active values");
});
Check("successful apply saves once refreshes once and rebases",()=>{
 double a=.7f;int saved=0,applied=0;var d=new HrSettingsDraft(new[]{Setting("a",()=>a,v=>a=v)});d.Read();Expect(!d.Dirty,"Float formatting dirtied untouched draft");
 d.Text["a"]="7";Expect(d.TryApply(()=>saved++,()=>applied++,out _)&&saved==1&&applied==1&&a==7&&!d.Dirty,"Apply did not rebase");
});
Check("closing discards draft and reopen reads latest preferences",()=>{
 double a=1;var d=new HrSettingsDraft(new[]{Setting("a",()=>a,v=>a=v)});d.Read();d.Text["a"]="9";d.Discard();a=3;d.Read();Expect(!d.Dirty&&d.Text["a"]=="3","Closed draft persisted");
});
#else
throw new Exception("HR draft implementation missing");
#endif
Check("locale comes from selected game language independent of OS",()=>{
 Expect(GameLanguageSource.Normalize("russian")=="ru"&&GameLanguageSource.Normalize("RU-ru")=="ru"&&GameLanguageSource.Normalize("english")=="en"&&GameLanguageSource.Normalize("unknown")=="en","Wrong game locale");
});
Console.WriteLine($"{n-failures}/{n} draft/locale checks passed");
return failures==0?0:1;
