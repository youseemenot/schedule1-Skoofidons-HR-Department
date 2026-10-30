using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.Management.UI;
using Il2CppScheduleOne.UI.Management;
using UnityEngine;

namespace SkoofidonsHRDepartment
{
    public static class PackagerUIFix
    {
        public static PackagerConfigPanel FindHandlerPanel(Component component) => component == null ? null :
            component.GetComponentInParent(Il2CppType.Of<PackagerConfigPanel>(), true)?.TryCast<PackagerConfigPanel>();
        public static void InitializeRouteOverride()
        {
            // Initialization only; Bind catches subsequent panel instances.
            foreach (var panel in Resources.FindObjectsOfTypeAll<ConfigPanel>()) ClipboardLayoutController.Apply(panel);
        }
        public static void EnsureHandlerPanel(PackagerConfigPanel panel) => ClipboardLayoutController.Apply(panel);
        public static void EnsureRouteScrollArea(RouteListFieldUI ui) => EnsureHandlerPanel(FindHandlerPanel(ui));
        public static void UpdateRouteCount() => RouteCounterController.RefreshAll();
        public static void ClearReferences()
        {
            ClipboardLayoutController.Clear();
            RouteCounterController.Clear();
            HandlerStationController.Clear();
        }
    }
}
