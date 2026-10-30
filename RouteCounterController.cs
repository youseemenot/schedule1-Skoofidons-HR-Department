using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.UI.Management;
using UnityEngine.Events;

namespace SkoofidonsHRDepartment
{
    public static class RouteCounterController
    {
        private sealed class View
        {
            public RouteListFieldUI UI;
            public IntPtr Key;
            public bool Started;
            public bool OwnsLabel;
            public bool ExpansionFailed;
            public readonly List<Action> DeleteCallbacks = new List<Action>();
        }

        private static readonly Dictionary<IntPtr, View> Views = new Dictionary<IntPtr, View>();

        private static bool IsHandler(RouteListFieldUI ui)
        {
            var fields = ui.Fields;
            if (fields == null || fields.Count == 0) return false;
            for (int i = 0; i < fields.Count; i++)
            {
                var parent = fields[i]?.ParentConfig;
                if (parent == null || parent.TryCast<PackagerConfiguration>() == null) return false;
            }
            return true;
        }

        private static View Track(RouteListFieldUI ui)
        {
            if (!Views.TryGetValue(ui.Pointer, out var view))
            {
                foreach (var old in new List<View>(Views.Values))
                    if (old.UI == null) Views.Remove(old.Key);
                view = new View { UI = ui, Key = ui.Pointer };
                Views.Add(ui.Pointer, view);
            }
            return view;
        }

        private static RouteCounterSnapshot Snapshot(RouteListFieldUI ui)
        {
            var fields = ui.Fields;
            if (fields == null || fields.Count != 1) return null;
            var field = fields[0];
            if (field == null || field.Routes == null) return null;
            return RouteCounterSnapshot.ForSelection(fields.Count, field.Routes.Count, field.MaxRoutes);
        }

        public static void Started(RouteListFieldUI ui)
        {
            if (ui == null) return;
            // Start may follow Bind. Remember it even before the UI has fields:
            // the native Start wires delete callbacks for entries existing then.
            Track(ui).Started = true;
            Refresh(ui);
        }

        public static void PrepareEntries(RouteListFieldUI ui)
        {
            if (ui == null || !IsHandler(ui)) return;
            var snapshot = Snapshot(ui);
            if (snapshot == null) return;
            var view = Track(ui);
            if (view.ExpansionFailed) return;
            var entries = ui.RouteEntries;
            if (entries == null || entries.Length == 0 || entries.Length >= snapshot.Used) return;
            var template = entries[0];
            if (template == null) return;

            // A larger limit alone does not allocate UI rows. Grow only for
            // existing data, and never shrink away over-limit assignments.
            var expanded = new Il2CppReferenceArray<RouteEntryUI>(snapshot.Used);
            int originalCount = (int)entries.Length;
            for (int i = 0; i < originalCount; i++) expanded[i] = entries[i];
            var created = new List<RouteEntryUI>();
            try
            {
                for (int i = originalCount; i < snapshot.Used; i++)
                {
                    var entry = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                    created.Add(entry);
                    entry.name = $"SkoofRouteEntry_{i}";
                    entry.gameObject.SetActive(false);
                    entry.onDeleteClicked = new UnityEvent();
                    if (view.Started)
                    {
                        Action delete = () => {
                            if (ui != null && entry != null) ui.EntryDeleteClicked(entry);
                        };
                        view.DeleteCallbacks.Add(delete);
                        entry.onDeleteClicked.AddListener(delete);
                    }
                    expanded[i] = entry;
                }
                ui.RouteEntries = expanded;
            }
            catch (Exception ex)
            {
                foreach (var entry in created)
                    if (entry != null) UnityEngine.Object.Destroy(entry.gameObject);
                view.ExpansionFailed = true;
                MelonLogger.Warning("Handler route row expansion failed; existing routes were preserved: " + ex.Message);
            }
        }

        public static void Refresh(RouteListFieldUI ui)
        {
            if (ui == null) return;
            if (!IsHandler(ui))
            {
                if (Views.TryGetValue(ui.Pointer, out var previous) && previous.OwnsLabel && ui.FieldLabel != null)
                {
                    ui.FieldLabel.text = ui.FieldText;
                    previous.OwnsLabel = false;
                }
                return;
            }
            var view = Track(ui);
            PackagerUIFix.EnsureRouteScrollArea(ui);
            var snapshot = Snapshot(ui);
            if (ui.FieldLabel != null)
            {
                ui.FieldLabel.text = snapshot == null ? ui.FieldText : snapshot.FormatLabel(ui.FieldText);
                view.OwnsLabel = snapshot != null;
            }
            // Native Refresh controls AddButton visibility, not interactable.
            // Leave its state alone; AddClicked separately enforces the limit.
        }

        public static bool CanAddRoute(RouteListFieldUI ui)
        {
            if (ui == null || !IsHandler(ui)) return true;
            var snapshot = Snapshot(ui);
            return snapshot != null && snapshot.CanAdd;
        }

        public static void RefreshAll()
        {
            foreach (var view in new List<View>(Views.Values))
            {
                var ui = view.UI;
                if (ui == null)
                {
                    Views.Remove(view.Key);
                    continue;
                }
                if (IsHandler(ui) && Snapshot(ui) != null)
                    ui.Refresh(ui.Fields[0].Routes);
                else
                    Refresh(ui);
            }
        }

        public static void Clear() => Views.Clear();
    }

    [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.Start))]
    internal static class RouteCounterStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RouteListFieldUI __instance) => RouteCounterController.Started(__instance);
    }

    [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.Bind))]
    internal static class RouteCounterBindPatch
    {
        [HarmonyPostfix]
        private static void Postfix(RouteListFieldUI __instance) => RouteCounterController.Refresh(__instance);
    }

    [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.Refresh))]
    internal static class RouteCounterRefreshPatch
    {
        [HarmonyPrefix]
        private static void Prefix(RouteListFieldUI __instance) => RouteCounterController.PrepareEntries(__instance);

        [HarmonyPostfix]
        private static void Postfix(RouteListFieldUI __instance) => RouteCounterController.Refresh(__instance);
    }

    [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.AddClicked))]
    internal static class RouteCounterAddPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(RouteListFieldUI __instance) => RouteCounterController.CanAddRoute(__instance);
    }
}
