using System.Reflection;
int count=0,failures=0;
void Check(string name,Action test){count++;try{test();Console.WriteLine("PASS "+name);}catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+(e.InnerException??e).Message);}}
dynamic Editor(string value){var type=typeof(Program).Assembly.GetType("Skoofidons.SharedSettings.V1.NumericEditor")??throw new Exception("Managed numeric editor missing");dynamic e=Activator.CreateInstance(type);e.Reset(value);return e;}
void Expect(bool x,string message){if(!x)throw new Exception(message);}
Check("select-all replaces current value with comma or dot number",()=>{dynamic e=Editor("1");e.SelectAll();e.Insert("0,5");Expect(e.Text=="0,5","Selected replacement failed");e.SelectAll();e.Insert("0.5");Expect(e.Text=="0.5","Dot lost");});
Check("arrows home end and shift preserve a selected range",()=>{dynamic e=Editor("12345");e.Move(-2,false);e.Move(-2,true);Expect(e.SelectedText=="23","Shift selection incorrect");e.Insert("7");Expect(e.Text=="1745","Replacement incorrect");e.Home(false);e.Insert("0");e.End(false);e.Insert("9");Expect(e.Text=="017459","Home/end incorrect");});
Check("backspace delete and selected deletion match standard fields",()=>{dynamic e=Editor("123");e.Backspace();Expect(e.Text=="12","Backspace");e.Home(false);e.Delete();Expect(e.Text=="2","Delete");e.SelectAll();e.Delete();Expect(e.Text=="","Selected delete");e.Backspace();e.Delete();Expect(e.Text=="","Empty edits");});
Check("clipboard range and click placement preserve unselected digits",()=>{dynamic e=Editor("1000");e.Place(1,false);e.Place(3,true);Expect(e.SelectedText=="00","Selection range");e.Insert("25");Expect(e.Text=="1250","Paste overwrote wrong range");});
Check("long integer and incomplete exponent remain draft text",()=>{dynamic e=Editor("21");e.SelectAll();e.Insert("2000000000");Expect(e.Text=="2000000000","Unwanted numeric cap");e.SelectAll();e.Insert("1e-");Expect(e.Text=="1e-","Incomplete input changed");});
Console.WriteLine($"{count-failures}/{count} numeric-editor checks passed");return failures==0?0:1;
