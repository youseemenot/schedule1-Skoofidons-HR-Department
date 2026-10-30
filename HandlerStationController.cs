using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.UI.Management;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using FieldList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Management.ObjectListField>;

namespace SkoofidonsHRDepartment
{
    public static class HandlerStationController
    {
        private sealed class View
        {
            public ObjectListFieldUI UI;
            public IntPtr Key;
            public int NativeRows;
            public bool Binding;
            public bool ExpansionFailed;
            // Retain managed delegates until native Bind replaces their events.
            public readonly List<Action> DeleteCallbacks = new List<Action>();
        }

        private static readonly Dictionary<IntPtr, View> Views = new Dictionary<IntPtr, View>();

        private static bool IsHandlerStations(FieldList fields)
        {
            if (fields == null || fields.Count == 0) return false;
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var parent = field?.ParentConfig?.TryCast<PackagerConfiguration>();
                if (parent?.Stations == null || parent.Stations.Pointer != field.Pointer) return false;
            }
            return true;
        }

        private static View Track(ObjectListFieldUI ui)
        {
            if (!Views.TryGetValue(ui.Pointer, out var view))
            {
                foreach (var old in new List<View>(Views.Values))
                    if (old.UI == null) Views.Remove(old.Key);
                view = new View { UI = ui, Key = ui.Pointer, NativeRows = (int)(ui.Entries?.Length ?? 0) };
                Views.Add(ui.Pointer, view);
            }
            return view;
        }

        private static Button RemoveButton(RectTransform entry) => entry?.Find("Remove")?.GetComponent<Button>();

        public static void BeforeBind(ObjectListFieldUI ui, FieldList fields)
        {
            if (ui == null) return;
            if (!Views.TryGetValue(ui.Pointer, out var view))
            {
                if (!IsHandlerStations(fields)) return;
                view = Track(ui);
            }
            view.Binding = true;
            // Native Bind calls Refresh first and then adds a delete listener to
            // every row. Clear only cloned row events before that registration.
            var entries = ui.Entries;
            if (entries != null)
            {
                // Restore the prefab's active roots before a possible multi-
                // selection bind; native Bind will apply its own restrictions.
                for (int i = 0; i < entries.Length; i++)
                    if (entries[i] != null) entries[i].gameObject.SetActive(true);
                for (int i = view.NativeRows; i < entries.Length; i++)
                {
                    var button = RemoveButton(entries[i]);
                    if (button != null) button.onClick = new Button.ButtonClickedEvent();
                }
            }
            view.DeleteCallbacks.Clear();
        }

        public static void AfterBind(ObjectListFieldUI ui)
        {
            if (ui == null) return;
            if (Views.TryGetValue(ui.Pointer, out var view)) view.Binding = false;
            RefreshLayout(ui, true);
        }

        public static void PrepareEntries(ObjectListFieldUI ui)
        {
            if (ui == null || !IsHandlerStations(ui.Fields) || ui.Fields.Count != 1) return;
            var selected = ui.Fields[0].SelectedObjects;
            var entries = ui.Entries;
            if (selected == null || entries == null || selected.Count <= entries.Length) return;
            var view = Track(ui);
            if (view.ExpansionFailed) return;
            var created = new List<RectTransform>();
            var callbacks = new List<Action>();
            void DestroyRow(RectTransform entry)
            {
                created.Remove(entry);
                if (entry != null) UnityEngine.Object.Destroy(entry.gameObject);
            }
            try
            {
                var original = new RectTransform[(int)entries.Length];
                for (int i = 0; i < original.Length; i++) original[i] = entries[i];
                // Allocate the interop array before creating any Unity objects.
                var expanded = new Il2CppReferenceArray<RectTransform>(selected.Count);
                var rows = StationRowExpansion.Expand(original, selected.Count, (template, index) => {
                    if (template == null) throw new InvalidOperationException("Station row template was destroyed.");
                    var entry = UnityEngine.Object.Instantiate(template, template.parent, false);
                    created.Add(entry);
                    try
                    {
                        entry.name = $"SkoofStationEntry_{index}";
                        entry.gameObject.SetActive(false);
                        var button = RemoveButton(entry);
                        if (button == null) throw new InvalidOperationException("Station row Remove button is missing.");
                        button.onClick = new Button.ButtonClickedEvent();
                        if (!view.Binding)
                        {
                            Action delete = () => {
                                if (ui != null && IsHandlerStations(ui.Fields) && ui.Fields.Count == 1 &&
                                    index < ui.Fields[0].SelectedObjects.Count)
                                    ui.RemoveEntryClicked(index);
                            };
                            callbacks.Add(delete);
                            button.onClick.AddListener(delete);
                        }
                        return entry;
                    }
                    catch { DestroyRow(entry); throw; }
                }, DestroyRow);
                for (int i = 0; i < rows.Length; i++) expanded[i] = rows[i];
                ui.Entries = expanded;
                view.DeleteCallbacks.AddRange(callbacks);
            }
            catch (Exception ex)
            {
                foreach (var entry in new List<RectTransform>(created)) DestroyRow(entry);
                view.ExpansionFailed = true;
                MelonLogger.Warning("Handler station row expansion failed; assignments were preserved: " + ex.Message);
            }
        }

        public static void RefreshLayout(ObjectListFieldUI ui, bool resetToTop = false)
        {
            if (ui == null || !IsHandlerStations(ui.Fields)) return;
            var panel = PackagerUIFix.FindHandlerPanel(ui);
            PackagerUIFix.EnsureHandlerPanel(panel);
            if (ui.Fields.Count == 1 && ui.Fields[0].SelectedObjects != null && ui.Entries != null)
            {
                // Native Refresh toggles children only. Root activation is also
                // needed for clones and for assignments above a lowered limit.
                var rows = new RectTransform[(int)ui.Entries.Length];
                for (int i = 0; i < rows.Length; i++) rows[i] = ui.Entries[i];
                StationRowExpansion.SetAssignedVisibility(rows, ui.Fields[0].SelectedObjects.Count,
                    (row, visible) => { if (row != null) row.gameObject.SetActive(visible); });
            }
            var scroll = ui.transform.Find("SkoofScrollArea")?.GetComponent<ScrollRect>();
            if (scroll?.content == null || scroll.viewport == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            var position = scroll.content.anchoredPosition;
            float maximum = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
            position.y = resetToTop ? 0f : Mathf.Clamp(position.y, 0f, maximum);
            scroll.content.anchoredPosition = position;
        }

        public static void Clear() => Views.Clear();
    }

    [HarmonyPatch(typeof(ObjectListFieldUI), nameof(ObjectListFieldUI.Bind))]
    internal static class HandlerStationBindPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ObjectListFieldUI __instance, FieldList __0) => HandlerStationController.BeforeBind(__instance, __0);

        [HarmonyPostfix]
        private static void Postfix(ObjectListFieldUI __instance) => HandlerStationController.AfterBind(__instance);
    }

    [HarmonyPatch(typeof(ObjectListFieldUI), nameof(ObjectListFieldUI.Refresh))]
    internal static class HandlerStationRefreshPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ObjectListFieldUI __instance) => HandlerStationController.PrepareEntries(__instance);

        [HarmonyPostfix]
        private static void Postfix(ObjectListFieldUI __instance) => HandlerStationController.RefreshLayout(__instance);
    }
}
