using System;
namespace Skoofidons.SharedSettings.V1
{
    // Managed editing policy avoids Unity's unavailable desktop TextEditor implementation.
    public sealed class NumericEditor
    {
        public string Text { get; private set; }="";
        public int Caret { get; private set; }
        public int Anchor { get; private set; }
        public int Start=>Math.Min(Caret,Anchor);
        public int Length=>Math.Abs(Caret-Anchor);
        public string SelectedText=>Text.Substring(Start,Length);
        public void Reset(string value){Text=value??"";Caret=Anchor=Text.Length;}
        public void SelectAll(){Anchor=0;Caret=Text.Length;}
        public void Place(int position,bool extend){Caret=Math.Clamp(position,0,Text.Length);if(!extend)Anchor=Caret;}
        public void Move(int delta,bool extend)
        {
            if(!extend&&Length>0){Place(delta<0?Start:Start+Length,false);return;}
            Place(Caret+delta,extend);
        }
        public void Home(bool extend)=>Place(0,extend);
        public void End(bool extend)=>Place(Text.Length,extend);
        public void Insert(string value)
        {
            value=(value??"").Replace("\r","").Replace("\n","");
            if(Text.Length-Length+value.Length>256)return;
            int start=Start;Text=Text.Remove(start,Length).Insert(start,value);Caret=Anchor=start+value.Length;
        }
        public void Backspace(){if(Length>0){Insert("");return;}if(Caret>0){Anchor=Caret-1;Insert("");}}
        public void Delete(){if(Length>0){Insert("");return;}if(Caret<Text.Length){Anchor=Caret+1;Insert("");}}
    }
}
