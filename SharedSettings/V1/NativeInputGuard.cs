using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Il2CppTMPro;

namespace Skoofidons.SharedSettings.V1
{
    public static class NativeInputGuard
    {
        public static bool CanOpen()
        {
            var current=EventSystem.current;
            var selected=current==null?null:current.currentSelectedGameObject;
            for(int i=0;selected!=null&&i<16;i++)
            {
                var field=selected.GetComponent<InputField>();
                if(field!=null&&field.isFocused)return false;
                var tmp=selected.GetComponent<TMP_InputField>();
                if(tmp!=null&&tmp.isFocused)return false;
                var parent=selected.transform.parent;
                selected=parent==null?null:parent.gameObject;
            }
            return true;
        }
    }
}
