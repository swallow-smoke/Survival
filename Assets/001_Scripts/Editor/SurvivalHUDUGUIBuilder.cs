#if UNITY_EDITOR
using _001_Scripts.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    internal static class SurvivalHUDUGUIBuilder
    {
        private const string RootName = "UGUI_HUDRoot";

        [MenuItem("Tools/Survival UI/Rebuild HUD Panel (UGUI)")]
        private static void RebuildFromMenu()
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<HUDPanel>(FindObjectsInactive.Include);
            if (panel) Build(panel, true);
        }

        private static void Build(HUDPanel panel, bool saveScene)
        {
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Build Survival HUD UGUI");
            ConfigureCanvas(panel.gameObject);
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);

            var root = Image(RootName, panel.transform, new Color(.015f, .09f, .13f, .90f));
            var rootRect = root.rectTransform;
            rootRect.anchorMin = rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(34, 34);
            rootRect.sizeDelta = new Vector2(330, 260);
            rootRect.localScale = Vector3.one;
            AddOutline(root.gameObject, new Color(.32f, .78f, .88f, .72f), 2);

            var accent = Image("Accent", root.transform, new Color(.08f, .84f, 1f));
            Stretch(accent.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, -52), new Vector2(5, 0));
            var title = Label("Title", root.transform, "LIFE SUPPORT", 17, FontStyle.Bold,
                new Color(.08f, .84f, 1f), TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(20, -44), new Vector2(-20, -10));
            var state = Label("SystemState", root.transform, "●  ONLINE", 11, FontStyle.Bold,
                new Color(.35f, .92f, .76f), TextAnchor.MiddleRight);
            Stretch(state.rectTransform, new Vector2(.5f, 1), new Vector2(1, 1),
                new Vector2(0, -42), new Vector2(-18, -12));
            var divider = Image("HeaderDivider", root.transform, new Color(.25f, .60f, .68f, .42f));
            Stretch(divider.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(14, -53), new Vector2(-14, -51));

            var oxygen = CreateStatRow(root.transform, 0, "O₂", "산소", new Color(.08f, .84f, 1f));
            var health = CreateStatRow(root.transform, 1, "+", "체력", new Color(.36f, .95f, .66f));
            var hunger = CreateStatRow(root.transform, 2, "◆", "배고픔", new Color(1f, .70f, .18f));
            var hydration = CreateStatRow(root.transform, 3, "●", "수분", new Color(.15f, .72f, 1f));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("oxygenFill").objectReferenceValue = oxygen.Fill;
            serialized.FindProperty("oxygenValue").objectReferenceValue = oxygen.Value;
            serialized.FindProperty("healthFill").objectReferenceValue = health.Fill;
            serialized.FindProperty("healthValue").objectReferenceValue = health.Value;
            serialized.FindProperty("hungerFill").objectReferenceValue = hunger.Fill;
            serialized.FindProperty("hungerValue").objectReferenceValue = hunger.Value;
            serialized.FindProperty("hydrationFill").objectReferenceValue = hydration.Fill;
            serialized.FindProperty("hydrationValue").objectReferenceValue = hydration.Value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SurvivalUIRoundedStyle.ApplyToRoot(panel.gameObject);

            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            if (saveScene) EditorSceneManager.SaveScene(panel.gameObject.scene);
            Debug.Log("[Survival UI] HUDPanel rebuilt as a serialized UGUI hierarchy.");
        }

        private static void ConfigureCanvas(GameObject hud)
        {
            var rect = hud.GetComponent<RectTransform>();
            if (rect) rect.localScale = Vector3.one;
            var scaler = hud.GetComponent<CanvasScaler>();
            if (!scaler) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
        }

        private static StatRefs CreateStatRow(Transform parent, int index, string icon, string title, Color color)
        {
            float top = -58 - index * 47;
            var row = Image(title + "Row", parent, new Color(0, 0, 0, index % 2 == 0 ? .13f : .05f));
            Stretch(row.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(14, top - 42), new Vector2(-14, top));

            var iconText = Label("Icon", row.transform, icon, 20, FontStyle.Bold, color, TextAnchor.MiddleCenter);
            Stretch(iconText.rectTransform, Vector2.zero, new Vector2(0, 1),
                new Vector2(8, 0), new Vector2(45, 0));
            var name = Label("Label", row.transform, title, 13, FontStyle.Bold,
                new Color(.86f, .94f, .96f), TextAnchor.MiddleLeft);
            Stretch(name.rectTransform, new Vector2(0, .45f), new Vector2(0, 1),
                new Vector2(48, -1), new Vector2(142, -1));
            var value = Label("Value", row.transform, "100%", 13, FontStyle.Bold, color, TextAnchor.MiddleRight);
            Stretch(value.rectTransform, new Vector2(1, .45f), Vector2.one,
                new Vector2(-78, -1), new Vector2(-10, -1));

            var track = Image("Track", row.transform, new Color(.12f, .25f, .28f, .88f));
            Stretch(track.rectTransform, Vector2.zero, new Vector2(1, 0),
                new Vector2(49, 7), new Vector2(-10, 13));
            var fill = Image("Fill", track.transform, color);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return new StatRefs(fill.rectTransform, value);
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Label(string name, Transform parent, string value, int size, FontStyle style,
            Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void AddOutline(GameObject go, Color color, float size)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
            outline.useGraphicAlpha = false;
        }

        private readonly struct StatRefs
        {
            public readonly RectTransform Fill;
            public readonly Text Value;

            public StatRefs(RectTransform fill, Text value)
            {
                Fill = fill;
                Value = value;
            }
        }
    }
}
#endif
