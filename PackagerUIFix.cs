using Il2CppScheduleOne.UI.Management;
using HarmonyLib;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkoofidonsHRDepartment
{
    public static class PackagerUIFix
    {
        public static void InitializeRouteOverride()
        {
            foreach (var panel in Resources.FindObjectsOfTypeAll<PackagerConfigPanel>())
                EnsureHandlerPanel(panel);
            var allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

            foreach (var textComponent in allTexts)
            {
                if (textComponent.text == null || textComponent.transform.parent == null) continue;

                // 2. Фикс для назначенных станций (Assigned)
                if (textComponent.text.StartsWith("Assigned"))
                {
                    if (textComponent.transform.parent.Find("Contents") != null)
                    {
                        // Handler uses its typed panel binding below, including
                        // panels instantiated after scene initialization.
                        if (textComponent.GetComponentInParent<PackagerConfigPanel>(true) != null) continue;
                        FixListUI(textComponent, textComponent.transform.parent, null, 330f);
                    }
                }
            }
        }

        public static void EnsureRouteScrollArea(RouteListFieldUI ui)
        {
            if (ui == null || ui.FieldLabel == null) return;
            var parent = ui.transform;
            if (parent.Find("Contents") == null && parent.Find("SkoofScrollArea") == null)
                parent = ui.FieldLabel.transform.parent;
            EnsureHandlerScroll(parent, 75f, 160f);
            // The game's Hint is fixed at x=105, immediately after "Routes".
            // Reserve the right edge for it rather than covering the counter.
            var hint = parent.Find("Hint")?.GetComponent<RectTransform>();
            if (hint != null)
            {
                hint.anchorMin = hint.anchorMax = new Vector2(1f, 1f);
                hint.anchoredPosition = new Vector2(-17.5f, -20f);
            }
            var label = ui.FieldLabel;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(-45f, 40f);
            labelRect.anchoredPosition = new Vector2(-22.5f, -20f);
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 26f;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        public static void EnsureHandlerPanel(PackagerConfigPanel panel)
        {
            if (panel == null) return;
            if (panel.StationsUI != null)
                EnsureHandlerScroll(panel.StationsUI.transform, 50f, 110f);
            EnsureRouteScrollArea(panel.RoutesUI);
        }

        private static void EnsureHandlerScroll(Transform parent, float top, float height)
        {
            if (parent == null || parent.Find("SkoofScrollArea") != null) return;
            var contents = parent.Find("Contents")?.GetComponent<RectTransform>();
            if (contents == null) return;
            var scrollObj = new GameObject("SkoofScrollArea", new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Type>(
                new[] { Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>() }));
            var viewport = scrollObj.GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(0f, height);
            viewport.anchoredPosition = new Vector2(0f, -top);
            scrollObj.AddComponent<RectMask2D>();
            // A transparent Graphic gives empty space a scroll-event target.
            scrollObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            contents.SetParent(viewport, false);
            contents.anchorMin = new Vector2(0f, 1f);
            contents.anchorMax = new Vector2(1f, 1f);
            contents.pivot = new Vector2(0.5f, 1f);
            contents.anchoredPosition = Vector2.zero;
            contents.sizeDelta = Vector2.zero;
            var fitter = contents.GetComponent<ContentSizeFitter>() ?? contents.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = contents;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 15f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contents);
            scroll.verticalNormalizedPosition = 1f;
        }

        public static void UpdateRouteCount() => RouteCounterController.RefreshAll();

        private static void FixListUI(TextMeshProUGUI label, Transform parent, string newTitle, float scrollHeight)
        {
            if (parent == null) return;

            // Теперь мы гарантированно меняем именно заголовок
            if (newTitle != null && label != null)
            {
                label.text = newTitle;
                label.raycastTarget = false;
            }

            Transform contents = parent.Find("Contents");
            if (contents == null || parent.Find("SkoofScrollArea") != null) return;

            // Расширяем родительский блок, чтобы заполнить пустоту на планшете
            LayoutElement le = parent.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = parent.gameObject.AddComponent<LayoutElement>();
            le.minHeight = scrollHeight + 40f;

            GameObject scrollObj = new GameObject("SkoofScrollArea");
            scrollObj.transform.SetParent(parent, false);

            RectTransform rt = scrollObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -35);
            rt.sizeDelta = new Vector2(0, scrollHeight);

            scrollObj.AddComponent<RectMask2D>();
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

            contents.SetParent(rt, false);

            ContentSizeFitter fitter = contents.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = contents.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contents.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 15f; // Чувствительность колесика мыши
        }

        public static void ClearReferences() => RouteCounterController.Clear();
    }

    [HarmonyPatch(typeof(PackagerConfigPanel), nameof(PackagerConfigPanel.BindInternal))]
    internal static class HandlerPanelLayoutPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PackagerConfigPanel __instance) => PackagerUIFix.EnsureHandlerPanel(__instance);
    }
}
