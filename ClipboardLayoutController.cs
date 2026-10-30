using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Management.UI;
using Il2CppScheduleOne.UI.Management;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace SkoofidonsHRDepartment
{
    public static class ClipboardLayoutController
    {
        private sealed class ListView
        {
            public ObjectListFieldUI Objects;
            public RectTransform Root, Contents, Viewport;
            public ScrollRect Scroll;
            public RectMask2D Mask;
            public Image Target;
            public float Heading;
            public Vector2 SavedPosition;
        }
        private sealed class View
        {
            public ConfigPanel Panel;
            public RectTransform Root, Content, Viewport;
            public ScrollRect Outer;
            public RectMask2D Mask;
            public Image Target;
            public readonly List<ListView> Lists = new List<ListView>();
            public float StartTop;
            public bool Busy;
            public bool OuterMode;
            public Vector2 SavedOuterPosition;
        }
        private static readonly Dictionary<IntPtr, View> Views = new Dictionary<IntPtr, View>();
        private static readonly Dictionary<IntPtr, ConfigPanel> Pending = new Dictionary<IntPtr, ConfigPanel>();
        private static DateTime nextRefresh;
        private static int warnings;

        public static void Apply(ConfigPanel panel)
        {
            if (panel == null || !Supported(panel)) return;
            try
            {
                var root = panel.GetComponent<RectTransform>();
                // Native inactive assets have zero height until parented.
                if (root == null) return;
                if (root.rect.height <= 1f) { Pending[panel.Pointer] = panel; return; }
                Pending.Remove(panel.Pointer);
                if (!Views.TryGetValue(panel.Pointer, out var view))
                {
                    view = Create(panel, root);
                    if (view == null) return;
                    Views.Add(panel.Pointer, view);
                }
                if (view.Busy) return;
                view.Busy = true;
                try { Layout(view); }
                finally { view.Busy = false; }
            }
            catch (Exception ex)
            {
                if (warnings++ < 3) MelonLogger.Warning("Adaptive clipboard layout: " + ex.Message);
            }
        }
        private static bool Supported(ConfigPanel panel) => panel.TryCast<PackagerConfigPanel>() != null ||
            panel.TryCast<BotanistConfigPanel>() != null || panel.TryCast<ChemistConfigPanel>() != null ||
            panel.TryCast<CleanerConfigPanel>() != null;

        private static View Create(ConfigPanel panel, RectTransform root)
        {
            var view = new View { Panel = panel, Root = root };
            // Clear drops managed references, while a surviving native panel or
            // an instantiated prefab may already contain generated wrappers.
            view.Viewport = root.Find("SkoofPanelScroll")?.TryCast<RectTransform>();
            view.Content = view.Viewport?.Find("Content")?.TryCast<RectTransform>();
            var originalParent = view.Content ?? root;
            foreach (var component in panel.GetComponentsInChildren(Il2CppType.Of<ObjectListFieldUI>(), true))
            {
                var list = component.TryCast<ObjectListFieldUI>();
                if (list != null) AddList(view, list.transform, list.FieldLabel, false, list);
            }
            var handler = panel.TryCast<PackagerConfigPanel>();
            if (handler?.RoutesUI != null) AddList(view, handler.RoutesUI.transform, handler.RoutesUI.FieldLabel, true);
            if (view.Lists.Count == 0) return null;
            view.Lists.Sort((a, b) => Top(originalParent, a.Root).CompareTo(Top(originalParent, b.Root)));
            view.StartTop = Math.Max(0f, Top(originalParent, view.Lists[0].Root));
            // Reserve Bed and Botanist's Supplies by native geometry.
            for (int i = 0; i < originalParent.childCount; i++)
            {
                var child = originalParent.GetChild(i).TryCast<RectTransform>();
                if (child == null || IsList(view, child) || child.name == "SkoofPanelScroll") continue;
                view.StartTop = Math.Max(view.StartTop, Top(originalParent, child) + child.rect.height + 12f);
            }
            if (view.Viewport != null && view.Content != null)
            {
                view.Mask = view.Viewport.GetComponent<RectMask2D>();
                view.Target = view.Viewport.GetComponent<Image>();
                view.Outer = view.Viewport.GetComponent<ScrollRect>();
                view.OuterMode = view.Outer.enabled;
                view.SavedOuterPosition = view.Content.anchoredPosition;
                return view;
            }
            view.Viewport = NewRect("SkoofPanelScroll", root);
            Stretch(view.Viewport);
            view.Mask = view.Viewport.gameObject.AddComponent<RectMask2D>();
            view.Target = view.Viewport.gameObject.AddComponent<Image>();
            view.Target.color = new Color(0f, 0f, 0f, 0f);
            view.Outer = view.Viewport.gameObject.AddComponent<ScrollRect>();
            view.Content = NewRect("Content", view.Viewport);
            Place(view.Content, 0f, root.rect.height);
            var children = new List<RectTransform>();
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i).TryCast<RectTransform>();
                if (child != null && child.Pointer != view.Viewport.Pointer) children.Add(child);
            }
            foreach (var child in children)
            {
                float top = Top(root, child), height = child.rect.height;
                child.SetParent(view.Content, false);
                Place(child, top, height);
            }
            Configure(view.Outer, view.Viewport, view.Content);
            return view;
        }
        private static bool IsList(View view, RectTransform child)
        {
            foreach (var list in view.Lists) if (list.Root.Pointer == child.Pointer) return true;
            return false;
        }
        private static void AddList(View view, Transform transform, TextMeshProUGUI label, bool routes, ObjectListFieldUI objects = null)
        {
            var root = transform.TryCast<RectTransform>();
            var existing = transform.Find("SkoofScrollArea")?.TryCast<RectTransform>();
            var contents = (transform.Find("Contents") ?? existing?.Find("Contents"))?.TryCast<RectTransform>();
            if (root == null || contents == null) return;
            float heading = 40f;
            if (routes)
                foreach (string name in new[] { "From", "To", "Filter" })
                {
                    var field = root.Find(name)?.TryCast<RectTransform>();
                    if (field != null) heading = Math.Max(heading, Top(root, field) + field.rect.height);
                }
            // Keep native controls at their original size as the root grows.
            for (int i = 0; existing == null && i < root.childCount; i++)
            {
                var child = root.GetChild(i).TryCast<RectTransform>();
                if (child == null || child.Pointer == contents.Pointer || child.name == "Blocker") continue;
                Place(child, Top(root, child), child.rect.height, false);
            }
            FitHeading(label, root, routes ? 17.5f : 57.5f, routes ? 45f : 80f);
            var viewport = existing ?? NewRect("SkoofScrollArea", root);
            var mask = viewport.GetComponent<RectMask2D>() ?? viewport.gameObject.AddComponent<RectMask2D>();
            var target = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            target.color = new Color(0f, 0f, 0f, 0f);
            var scroll = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
            if (existing == null)
            {
                contents.SetParent(viewport, false);
                Place(contents, 0f, 0f);
            }
            var fitter = contents.GetComponent<ContentSizeFitter>() ?? contents.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Configure(scroll, viewport, contents);
            // The native multi-selection blocker must cover row buttons as
            // well as the Add control after the viewport is appended.
            root.Find("Blocker")?.SetAsLastSibling();
            root.Find("MultiEditBlocker")?.SetAsLastSibling();
            view.Lists.Add(new ListView { Root = root, Contents = contents, Viewport = viewport,
                Objects = objects, Scroll = scroll, Mask = mask, Target = target, Heading = heading + 4f,
                SavedPosition = contents.anchoredPosition });
        }
        private static void Layout(View view)
        {
            int count = view.Lists.Count;
            var headings = new float[count];
            var preferred = new float[count];
            for (int i = 0; i < count; i++)
            {
                var list = view.Lists[i];
                // Native Refresh hides row children only. Deactivate unused
                // roots as well so layout groups do not reserve blank rows.
                var fields = list.Objects?.Fields;
                var entries = list.Objects?.Entries;
                if (fields != null && fields.Count == 1 && fields[0]?.SelectedObjects != null && entries != null)
                {
                    int assigned = fields[0].SelectedObjects.Count;
                    for (int row = 0; row < entries.Length; row++)
                        if (entries[row] != null && entries[row].gameObject.activeSelf != (row < assigned))
                            entries[row].gameObject.SetActive(row < assigned);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(list.Contents);
                headings[i] = list.Heading;
                preferred[i] = Math.Max(list.Contents.rect.height, LayoutUtility.GetPreferredHeight(list.Contents));
            }
            var budget = ClipboardLayoutPolicy.Calculate(view.Root.rect.height, view.StartTop, headings, preferred);
            bool changedMode = view.OuterMode != budget.OuterScroll;
            if (changedMode)
            {
                if (budget.OuterScroll) view.Content.anchoredPosition = view.SavedOuterPosition;
                else
                {
                    view.SavedOuterPosition = view.Content.anchoredPosition;
                    view.Content.anchoredPosition = Vector2.zero;
                }
            }
            view.Mask.enabled = budget.OuterScroll;
            view.Target.raycastTarget = budget.OuterScroll;
            view.Outer.enabled = budget.OuterScroll;
            view.Outer.vertical = budget.OuterScroll;
            view.Content.sizeDelta = new Vector2(0f, budget.ContentHeight);
            for (int i = 0; i < count; i++)
            {
                var list = view.Lists[i];
                var section = budget.Sections[i];
                Place(list.Root, section.Top, list.Heading + section.Height);
                Place(list.Viewport, list.Heading, section.Height);
                list.Mask.enabled = !budget.OuterScroll;
                list.Target.raycastTarget = !budget.OuterScroll;
                list.Scroll.vertical = !budget.OuterScroll;
                list.Scroll.enabled = !budget.OuterScroll;
                if (changedMode)
                {
                    if (budget.OuterScroll) list.SavedPosition = list.Contents.anchoredPosition;
                    else list.Contents.anchoredPosition = list.SavedPosition;
                }
                if (budget.OuterScroll) list.Contents.anchoredPosition = Vector2.zero;
                else Clamp(list.Scroll);
            }
            if (budget.OuterScroll) Clamp(view.Outer);
            view.OuterMode = budget.OuterScroll;
        }
        public static void RefreshTracked()
        {
            if (DateTime.UtcNow < nextRefresh) return;
            nextRefresh = DateTime.UtcNow.AddMilliseconds(200);
            foreach (var pair in new List<KeyValuePair<IntPtr, ConfigPanel>>(Pending))
            {
                if (pair.Value == null) Pending.Remove(pair.Key);
                else if (pair.Value.gameObject.activeInHierarchy) Apply(pair.Value);
            }
            foreach (var pair in new List<KeyValuePair<IntPtr, View>>(Views))
            {
                if (pair.Value.Panel == null || pair.Value.Root == null) Views.Remove(pair.Key);
                else if (pair.Value.Panel.gameObject.activeInHierarchy) Apply(pair.Value.Panel);
            }
        }
        public static void Clear() { Views.Clear(); Pending.Clear(); nextRefresh = default; warnings = 0; }
        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, new Il2CppReferenceArray<Il2CppSystem.Type>(new[] { Il2CppType.Of<RectTransform>() }));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
        private static float Top(RectTransform parent, RectTransform child) => parent.rect.yMax -
            parent.InverseTransformPoint(child.TransformPoint(new Vector3(0f, child.rect.yMax, 0f))).y;
        private static void Place(RectTransform rect, float top, float height, bool stretchWidth = true)
        {
            float left = rect.anchoredPosition.x;
            rect.anchorMin = new Vector2(stretchWidth ? 0f : rect.anchorMin.x, 1f);
            rect.anchorMax = new Vector2(stretchWidth ? 1f : rect.anchorMax.x, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(stretchWidth ? 0f : rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(stretchWidth ? 0f : left, -top);
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        private static void Configure(ScrollRect scroll, RectTransform viewport, RectTransform content)
        {
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 15f;
        }
        private static void Clamp(ScrollRect scroll)
        {
            var position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height));
            scroll.content.anchoredPosition = position;
        }
        private static void FitHeading(TextMeshProUGUI label, Transform parent, float hintInset, float labelInset)
        {
            if (label == null) return;
            var hint = parent.Find("Hint")?.TryCast<RectTransform>();
            if (hint != null)
            {
                hint.anchorMin = hint.anchorMax = new Vector2(1f, 1f);
                hint.pivot = new Vector2(0.5f, 0.5f);
                hint.anchoredPosition = new Vector2(-hintInset, -20f);
            }
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(-labelInset, 40f);
            rect.anchoredPosition = new Vector2(-labelInset / 2f, -20f);
            label.enableWordWrapping = false; label.enableAutoSizing = true;
            label.fontSizeMin = 18f; label.fontSizeMax = 26f;
            label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
        }
    }
    [HarmonyPatch(typeof(ConfigPanel), nameof(ConfigPanel.Bind))]
    internal static class ClipboardPanelBindPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ConfigPanel __instance) => ClipboardLayoutController.Apply(__instance);
    }
}
