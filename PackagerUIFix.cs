using System;
using System.Linq;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkoofidonsHRDepartment
{
    public static class PackagerUIFix
    {
        public static void InitializeRouteOverride()
        {
            var allTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

            foreach (var textComponent in allTexts)
            {
                if (textComponent.text == null || textComponent.transform.parent == null) continue;

                // 1. Фикс для маршрутов (Handler)
                if (textComponent.text.Contains("Routes"))
                {
                    if (textComponent.transform.parent.Find("Contents") != null)
                    {
                        // Добавляем счетчик из конфига
                        string routeTitle = $"Routes (Max: {Config.PackagerMaxRoutes.Value})";
                        // Передаем сам textComponent, чтобы точно изменить нужный текст, а не иконку
                        FixListUI(textComponent, textComponent.transform.parent, routeTitle, 160f);
                    }
                }

                // 2. Фикс для назначенных станций (Assigned)
                if (textComponent.text.StartsWith("Assigned"))
                {
                    if (textComponent.transform.parent.Find("Contents") != null)
                    {
                        bool isHandler = textComponent.transform.parent.parent.GetComponentsInChildren<TextMeshProUGUI>()
                            .Any(t => t.text != null && t.text.Contains("Routes"));

                        // Если Handler - 130px, если другие (Botanist/Chemist/Cleaner) - 330px
                        float scrollHeight = isHandler ? 130f : 330f;

                        FixListUI(textComponent, textComponent.transform.parent, null, scrollHeight);
                    }
                }
            }
        }

        public static void UpdateRouteCount() { }

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

        public static void ClearReferences() { }
    }
}