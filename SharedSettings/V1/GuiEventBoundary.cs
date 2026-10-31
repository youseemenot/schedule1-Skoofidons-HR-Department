using System;
namespace Skoofidons.SharedSettings.V1
{
    public static class GuiEventBoundary
    {
        public static void End(Action nativeExit)
        {
            try { nativeExit(); }
            // Unity already marked this GUI event as exiting. Do not turn its intentional
            // IL2CPP-to-CLR exception into a MelonLoader error, and do not draw anything after it.
            catch(Exception e) when(MenuSession.IsGuiExit(e)) { }
        }
    }
}
