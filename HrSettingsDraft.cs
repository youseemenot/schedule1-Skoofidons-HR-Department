using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace SkoofidonsHRDepartment
{
    public sealed class NumericSetting
    {
        public string Id { get; } public int Tab { get; } public string Ru { get; } public string En { get; }
        public Func<double> Get { get; } public Action<double> Set { get; } public bool Integer { get; } public double Minimum { get; }
        public NumericSetting(string id,int tab,string ru,string en,Func<double> get,Action<double> set,bool integer=false,double minimum=0)
        {Id=id;Tab=tab;Ru=ru;En=en;Get=get;Set=set;Integer=integer;Minimum=minimum;}
        public bool TryParse(string text,out double value)
        {
            value=0;
            return double.TryParse((text??"").Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&
                double.IsFinite(value)&&value>=Minimum&&value<=(Integer?(double)int.MaxValue:float.MaxValue)&&
                (!Integer||value==Math.Truncate(value))&&(Integer||Minimum<=0||(float)value>0);
        }
        public string Format(double value)=>Integer?value.ToString("0",CultureInfo.InvariantCulture):((float)value).ToString("G",CultureInfo.InvariantCulture);
    }
    public sealed class HrSettingsDraft
    {
        public IReadOnlyList<NumericSetting> Settings { get; }
        public Dictionary<string,string> Text { get; }=new();
        private readonly Dictionary<string,string> baseline=new();
        public HrSettingsDraft(IEnumerable<NumericSetting> settings){Settings=settings.ToArray();}
        public void Read(){Text.Clear();baseline.Clear();foreach(var s in Settings)Text[s.Id]=baseline[s.Id]=s.Format(s.Get());}
        public bool Dirty=>Settings.Any(s=>!Text.TryGetValue(s.Id,out var text)||!s.TryParse(text,out var v)||s.Format(v)!=baseline.GetValueOrDefault(s.Id));
        public void Discard(){Text.Clear();baseline.Clear();}
        public bool TryValidate(out string error)
        {
            foreach(var s in Settings)if(!Text.TryGetValue(s.Id,out var text)||!s.TryParse(text,out _)){error=s.Id;return false;}
            error=null;return true;
        }
        public bool TryApply(Action save,Action refresh,out string error)
        {
            if(!TryValidate(out error))return false;
            var originals=Settings.Select(s=>s.Get()).ToArray();
            try{foreach(var s in Settings){s.TryParse(Text[s.Id],out var v);s.Set(v);}save();}
            catch(Exception e){for(int i=0;i<Settings.Count;i++)Settings[i].Set(originals[i]);error=e.Message;return false;}
            Read();try{refresh();error=null;return true;}catch(Exception e){error=e.Message;return false;}
        }
    }
}
