using System;
namespace Skoofidons.SharedSettings.V1
{
    public sealed class ResetConfirmation
    {
        public bool Pending {get;private set;}
        public int Tab {get;private set;}=-1;
        public string Error {get;private set;}
        public void Request(int tab)
        {
            if(Pending)return;
            if(tab<0)throw new ArgumentOutOfRangeException(nameof(tab));
            Pending=true;Tab=tab;Error=null;
        }
        public void Cancel(){Pending=false;Tab=-1;Error=null;}
        public bool Confirm(Action<int> apply)
        {
            if(!Pending)return false;
            try{apply(Tab);Cancel();return true;}
            catch(Exception e){Error=e.Message;return false;}
        }
    }
}
