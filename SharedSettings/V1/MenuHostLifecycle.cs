#nullable enable
using System;
namespace Skoofidons.SharedSettings.V1
{
    // Owns bootstrap/restart state; failed native releases remain attached until recovered.
    public sealed class MenuHostLifecycle
    {
        public MenuSession? Session {get;private set;}
        public bool IsHost {get;private set;}
        public bool Prepare(int frame)
        {
            if(IsHost&&Session!=null)return false;
            if(Session!=null)
            {
                Session.Abort(frame);
                if(Session.HasLease)throw new InvalidOperationException("Previous settings input state is still being released.");
                Session=null;
            }
            return true;
        }
        public void Activate(MenuSession session)
        {
            if(Session!=null||IsHost)throw new InvalidOperationException("Settings host is already active or still cleaning up.");
            Session=session??throw new ArgumentNullException(nameof(session));IsHost=true;
        }
        public void Stop(int frame)
        {
            try{Session?.Abort(frame);}
            finally{IsHost=false;if(Session?.HasLease!=true)Session=null;}
        }
    }
}
