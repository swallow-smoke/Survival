#if UNITY_EDITOR
using _001_Scripts.UI;
using _001_Scripts.UI.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalCraftUGUIBuilder
    {
        private const string RootName = "UGUI_CraftRoot";
        private const string PrefabPath = "Assets/002_Prefabs/UI/BlueprintSlot.prefab";
        private const int LayoutVersion = 1;

        static SurvivalCraftUGUIBuilder() => EditorApplication.delayCall += RebuildIfNeeded;

        private static void RebuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            var panel = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            if (!panel) return;
            var version = new SerializedObject(panel).FindProperty("editorLayoutVersion");
            if (version != null && version.intValue >= LayoutVersion) return;
            Build(panel, true);
        }

        [MenuItem("Tools/Survival UI/Rebuild Craft Panel (UGUI)")]
        private static void RebuildFromMenu()
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            if (!panel) return;
            var marker = panel.transform.Find(RootName);
            if (marker) UnityEngine.Object.DestroyImmediate(marker.gameObject);
            Build(panel, true);
        }

        private static void Build(CraftPanel panel, bool saveScene)
        {
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Build Survival Craft UGUI");
            panel.transform.localScale = Vector3.one;
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);

            ConfigurePanelCanvas(panel.gameObject);
            var root = Image(RootName, panel.transform, new Color(0.005f, 0.025f, 0.04f, 0.60f));
            Stretch(root.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = Image("CraftCard", root.transform, new Color(0.035f, 0.16f, 0.20f, 0.98f));
            Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500, 760), new Vector2(0, -24));
            Outline(card.gameObject, new Color(0.32f, 0.78f, 0.88f, 0.75f), 2);

            var header = Image("Header", card.transform, new Color(0.05f, 0.30f, 0.38f, 0.98f));
            Stretch(header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -70), Vector2.zero);
            var accent = Image("Accent", header.transform, new Color(0.08f, 0.84f, 1f));
            Stretch(accent.rectTransform, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(6, 0));
            var title = Text("Title", header.transform, "제작", 27, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(28, 0), new Vector2(-420, 0));
            var online = Text("StationState", header.transform, "FABRICATION TERMINAL   •   ONLINE", 11,
                FontStyle.Bold, new Color(0.55f, 0.87f, 0.92f), TextAnchor.MiddleRight);
            Stretch(online.rectTransform, new Vector2(.4f, 0), Vector2.one, Vector2.zero, new Vector2(-78, 0));
            var close = Button("CloseButton", header.transform, "×", new Color(0, 0, 0, 0), 30);
            Stretch(close.GetComponent<RectTransform>(), new Vector2(1, 0), Vector2.one,
                new Vector2(-68, 6), new Vector2(-8, -6));

            var listPanel = Image("RecipeListPanel", card.transform, new Color(0.015f, 0.08f, 0.105f, 0.82f));
            Stretch(listPanel.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 88), new Vector2(-945, -94));
            Outline(listPanel.gameObject, new Color(0.22f, 0.55f, 0.62f, 0.55f), 1);
            var listTitle = Text("Title", listPanel.transform, "제작 설계도", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(listTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(16, -40), new Vector2(-16, -5));
            var viewport = Image("Viewport", listPanel.transform, new Color(0, 0, 0, 0));
            Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -48));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25;

            var detail = Image("RecipeDetailsPanel", card.transform, new Color(0.02f, 0.10f, 0.13f, 0.88f));
            Stretch(detail.rectTransform, Vector2.zero, Vector2.one, new Vector2(575, 88), new Vector2(-20, -94));
            Outline(detail.gameObject, new Color(0.22f, 0.55f, 0.62f, 0.55f), 1);
            var detailTitle = Text("Title", detail.transform, "설계도 정보", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(detailTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(18, -40), new Vector2(-18, -5));

            var preview = Image("Preview", detail.transform, new Color(0.045f, 0.22f, 0.26f, 0.92f));
            Stretch(preview.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -190), new Vector2(158, -50));
            Outline(preview.gameObject, new Color(0.32f, 0.75f, 0.82f, 0.65f), 1);
            var glyph = Text("Glyph", preview.transform, "◆", 62, FontStyle.Bold,
                new Color(0.08f, 0.84f, 1f), TextAnchor.MiddleCenter);
            Stretch(glyph.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));
            var detailName = Text("ItemName", detail.transform, "설계도를 선택하세요", 23, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            Stretch(detailName.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(176, -103), new Vector2(-18, -51));
            var detailMeta = Text("ItemMeta", detail.transform, "NO BLUEPRINT SELECTED", 12, FontStyle.Bold,
                new Color(0.08f, 0.84f, 1f), TextAnchor.MiddleLeft);
            Stretch(detailMeta.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(176, -140), new Vector2(-18, -104));
            var description = Text("Description", detail.transform, "왼쪽 목록에서 제작할 아이템을 선택하세요.", 13,
                FontStyle.Normal, new Color(0.61f, 0.76f, 0.80f), TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(description.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(176, -187), new Vector2(-18, -143));
            var divider = Image("Divider", detail.transform, new Color(0.28f, 0.62f, 0.68f, 0.45f));
            Stretch(divider.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -214), new Vector2(-18, -212));
            var ingredientsTitle = Text("IngredientsTitle", detail.transform, "필요 재료", 14, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            Stretch(ingredientsTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(18, -252), new Vector2(-18, -218));
            var ingredientParent = CreateRect("IngredientContent", detail.transform);
            Stretch(ingredientParent, Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -258));
            var ingredientLayout = ingredientParent.gameObject.AddComponent<VerticalLayoutGroup>();
            ingredientLayout.spacing = 8;
            ingredientLayout.childControlHeight = false;
            ingredientLayout.childControlWidth = true;
            ingredientLayout.childForceExpandHeight = false;
            ingredientLayout.childForceExpandWidth = true;
            var ingredientTemplate = BuildIngredientTemplate(ingredientParent);
            ingredientTemplate.SetActive(false);

            var result = Text("ResultText", card.transform, "제작할 설계도를 선택하세요.", 12, FontStyle.Bold,
                new Color(0.61f, 0.76f, 0.80f), TextAnchor.MiddleLeft);
            Stretch(result.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(22, 20), new Vector2(-230, 66));
            var craft = Button("CraftButton", card.transform, "⚙  제작하기", new Color(0.03f, 0.62f, 0.76f, 1), 15);
            Stretch(craft.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-210, 18), new Vector2(-20, 68));

            var prefab = BuildBlueprintPrefab();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("blueprintSlotPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("listParent").objectReferenceValue = content;
            serialized.FindProperty("detailName").objectReferenceValue = detailName;
            serialized.FindProperty("detailMeta").objectReferenceValue = detailMeta;
            serialized.FindProperty("detailDescription").objectReferenceValue = description;
            serialized.FindProperty("previewGlyph").objectReferenceValue = glyph;
            serialized.FindProperty("ingredientParent").objectReferenceValue = ingredientParent;
            serialized.FindProperty("ingredientTemplate").objectReferenceValue = ingredientTemplate;
            serialized.FindProperty("resultText").objectReferenceValue = result;
            serialized.FindProperty("craftButton").objectReferenceValue = craft;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("editorLayoutVersion").intValue = LayoutVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var inventoryPanel = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (inventoryPanel) SurvivalUIInputBindingEditor.EnsureNavigationForPanels(inventoryPanel, panel);
            SurvivalUIRoundedStyle.ApplyToRoot(panel.gameObject);

            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            if (saveScene) EditorSceneManager.SaveScene(panel.gameObject.scene);
            Debug.Log("[Survival UI] CraftPanel rebuilt as serialized UGUI hierarchy.");
        }

        private static GameObject BuildIngredientTemplate(Transform parent)
        {
            var row = Image("IngredientTemplate", parent, new Color(0.04f, 0.18f, 0.21f, 0.88f));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            var icon = Text("Icon", row.transform, "◆", 22, FontStyle.Bold,
                new Color(0.08f, 0.84f, 1f), TextAnchor.MiddleCenter);
            Stretch(icon.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(8, 4), new Vector2(52, -4));
            var name = Text("Name", row.transform, "재료", 13, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(62, 4), new Vector2(-140, -4));
            var count = Text("Count", row.transform, "0 / 0", 13, FontStyle.Bold,
                new Color(0.35f, 0.95f, 0.66f), TextAnchor.MiddleRight);
            Stretch(count.rectTransform, new Vector2(.65f, 0), Vector2.one, new Vector2(0, 4), new Vector2(-14, -4));
            return row.gameObject;
        }

        private static GameObject BuildBlueprintPrefab()
        {
            EnsureFolder("Assets/002_Prefabs/UI");
            var root = new GameObject("BlueprintSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(LayoutElement), typeof(BlueprintSlot));
            root.layer = 5;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 72);
            root.GetComponent<Image>().color = new Color(0.055f, 0.24f, 0.28f, 0.96f);
            root.GetComponent<LayoutElement>().preferredHeight = 72;
            Outline(root, new Color(0.27f, 0.53f, 0.57f, 0.55f), 1);
            var icon = Text("Icon", root.transform, "◆", 25, FontStyle.Bold,
                new Color(0.08f, 0.84f, 1f), TextAnchor.MiddleCenter);
            Stretch(icon.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(10, 8), new Vector2(62, -8));
            var name = Text("Name", root.transform, "설계도", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(name.rectTransform, Vector2.zero, Vector2.one, new Vector2(72, 25), new Vector2(-90, -8));
            var meta = Text("Meta", root.transform, "LEVEL 0   •   1 SEC", 10, FontStyle.Bold,
                new Color(0.61f, 0.76f, 0.80f), TextAnchor.MiddleLeft);
            Stretch(meta.rectTransform, Vector2.zero, Vector2.one, new Vector2(72, 6), new Vector2(-90, -39));
            var state = Text("State", root.transform, "READY", 9, FontStyle.Bold,
                new Color(0.61f, 0.76f, 0.80f), TextAnchor.MiddleRight);
            Stretch(state.rectTransform, new Vector2(.65f, 0), Vector2.one, new Vector2(0, 4), new Vector2(-12, -4));

            var view = root.GetComponent<BlueprintSlot>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("background").objectReferenceValue = root.GetComponent<Image>();
            serialized.FindProperty("iconText").objectReferenceValue = icon;
            serialized.FindProperty("nameText").objectReferenceValue = name;
            serialized.FindProperty("metaText").objectReferenceValue = meta;
            serialized.FindProperty("stateText").objectReferenceValue = state;
            serialized.FindProperty("selectButton").objectReferenceValue = root.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigurePanelCanvas(GameObject go)
        {
            var scaler = go.GetComponent<CanvasScaler>();
            if (!scaler) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
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

        private static Text Text(string name, Transform parent, string value, int size, FontStyle style,
            Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Color color, int size)
        {
            var image = Image(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", image.transform, label, size, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
            return button;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }

        private static void Outline(GameObject go, Color color, float size)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
            outline.useGraphicAlpha = false;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
