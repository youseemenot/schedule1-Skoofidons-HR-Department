int checks=0,failed=0;
void Check(string name,Action test){checks++;try{test();Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Expect(bool value,string why){if(!value)throw new Exception(why);}
dynamic Dialog()=>Activator.CreateInstance(typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.ResetConfirmation")??throw new Exception("Confirmation model missing"));
Check("request and cancel never apply a reset",()=>{dynamic d=Dialog();d.Request(2);Expect(d.Pending&&d.Tab==2,"Request not captured");d.Cancel();int calls=0;Expect(!d.Confirm((Action<int>)(_=>calls++))&&calls==0&&!d.Pending,"Cancel applied defaults");});
Check("confirm applies original captured tab exactly once",()=>{dynamic d=Dialog();d.Request(1);d.Request(3);int calls=0,tab=-1;Expect(d.Confirm((Action<int>)(t=>{calls++;tab=t;})),"Confirm rejected");d.Confirm((Action<int>)(_=>calls++));Expect(calls==1&&tab==1&&!d.Pending,"Retarget or duplicate application");});
Check("application failure remains visible and cancelable",()=>{dynamic d=Dialog();d.Request(0);Expect(!d.Confirm((Action<int>)(_=>throw new IOException("Failed")))&&d.Pending&&d.Error=="Failed","Failure lost");d.Cancel();Expect(!d.Pending&&d.Error==null,"Error survived cancel");});
Check("closing and reopening drops previous scope",()=>{dynamic d=Dialog();d.Request(3);d.Cancel();d.Request(0);int tab=-1;d.Confirm((Action<int>)(t=>tab=t));Expect(tab==0,"Stale tab restored");});
Console.WriteLine($"{checks-failed}/{checks} reset-confirmation checks passed");return failed==0?0:1;
