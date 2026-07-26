#if UNITY_EDITOR
using _001_Scripts.Controller;
using _001_Scripts.Data.SOJ;
using _001_Scripts.UI;
using _001_Scripts.UI.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalInventoryUGUIBuilder
    {
        private const string RootName = "UGUI_InventoryRoot";
        private const string PrefabPath = "Assets/002_Prefabs/UI/InventorySlot.prefab";
        private const int SlotCount = 40;
        private const int LayoutVersion = 4;

        static SurvivalInventoryUGUIBuilder()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += RebuildIfNeeded;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += RebuildIfNeeded;
        }

        private static void RebuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            var panel = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (!panel) return;
            var version = new SerializedObject(panel).FindProperty("editorLayoutVersion");
            if (version != null && version.intValue >= LayoutVersion) return;
            Build(panel, true);
        }

        [MenuItem("Tools/Survival UI/Rebuild Inventory Panel (UGUI)")]
        private static void RebuildFromMenu()
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (panel) Build(panel, true);
        }

        private static void Build(InventoryPanel panel, bool saveScene)
        {
            EnsureCraftController(panel);
            Undo.RegisterFullObjectHierarchyUndo(panel.gameObject, "Build Survival Inventory UGUI");
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);

            var panelRect = panel.GetComponent<RectTransform>();
            if (panelRect)
            {
                Stretch(panelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                panelRect.localScale = Vector3.one;
            }
            ConfigureInteractionCanvas(panel.gameObject);

            var root = Image(RootName, panel.transform, new Color(.012f, .006f, .035f, .24f));
            Stretch(root.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = Image("InventoryCard", root.transform, new Color(.04f, .02f, .09f, .72f));
            Anchor(card.rectTransform, new Vector2(.5f, .5f), new Vector2(1220, 700), new Vector2(0, -24));
            AddOutline(card.gameObject, new Color(.62f, .49f, 1f, .48f), 2);

            var header = Image("Header", card.transform, new Color(.11f, .055f, .22f, .64f));
            Stretch(header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -78), Vector2.zero);
            var accent = Image("Accent", header.transform, new Color(.52f, .72f, 1f, .9f));
            Stretch(accent.rectTransform, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(5, 0));
            var eyebrow = Label("Meta", header.transform, "ABYSSAL SUIT  //  STORAGE LINK", 10,
                FontStyle.Bold, new Color(.68f, .61f, .88f), TextAnchor.MiddleLeft);
            Stretch(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(.55f, 1),
                new Vector2(28, -28), new Vector2(0, -8));
            var title = Label("Title", header.transform, "생존 인벤토리", 25, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, Vector2.zero, new Vector2(.55f, 1), new Vector2(28, 4), new Vector2(0, -28));
            var capacity = Label("Capacity", header.transform, "40 SLOTS   •   TAB 닫기", 11, FontStyle.Bold,
                new Color(.67f, .78f, 1f), TextAnchor.MiddleRight);
            Stretch(capacity.rectTransform, new Vector2(.5f, 0), Vector2.one, Vector2.zero, new Vector2(-82, 0));
            var close = MakeButton("CloseButton", header.transform, "×", new Color(0, 0, 0, 0), 30);
            Stretch(close.GetComponent<RectTransform>(), new Vector2(1, 0), Vector2.one,
                new Vector2(-68, 6), new Vector2(-8, -6));
            var itemArea = Image("SlotBay", card.transform, new Color(.045f, .025f, .10f, .58f));
            Stretch(itemArea.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 88), new Vector2(-438, -94));
            AddOutline(itemArea.gameObject, new Color(.55f, .48f, .91f, .34f), 1);
            var itemHeader = Label("Title", itemArea.transform, "보관 슬롯", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(itemHeader.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(18, -42), new Vector2(-16, -5));
            var slotMeta = Label("Meta", itemArea.transform, "DRAG TO REORDER", 9, FontStyle.Bold,
                new Color(.65f, .58f, .82f), TextAnchor.MiddleRight);
            Stretch(slotMeta.rectTransform, new Vector2(.55f, 1), Vector2.one,
                new Vector2(0, -42), new Vector2(-18, -5));

            var viewport = Image("SlotViewport", itemArea.transform, new Color(0, 0, 0, 0));
            Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -50));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = CreateRect("SlotContent", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.cellSize = new Vector2(128, 104);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;

            var scrollbarTrack = Image("ScrollbarTrack", itemArea.transform, new Color(.10f, .06f, .18f, .55f));
            Stretch(scrollbarTrack.rectTransform, new Vector2(1, 0), Vector2.one,
                new Vector2(-12, 18), new Vector2(-5, -54));
            var scrollbar = scrollbarTrack.gameObject.AddComponent<Scrollbar>();
            var scrollbarHandle = Image("ScrollbarHandle", scrollbarTrack.transform,
                new Color(.60f, .48f, .96f, .72f));
            Stretch(scrollbarHandle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scrollbar.handleRect = scrollbarHandle.rectTransform;
            scrollbar.targetGraphic = scrollbarHandle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4f;

            var slotPrefab = BuildSlotPrefab();
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, content);
                slot.name = $"Slot_{i:00}";
            }

            var detail = Image("ItemDetails", card.transform, new Color(.055f, .03f, .12f, .62f));
            Stretch(detail.rectTransform, new Vector2(1, 0), Vector2.one,
                new Vector2(-418, 88), new Vector2(-20, -94));
            AddOutline(detail.gameObject, new Color(.55f, .48f, .91f, .34f), 1);
            var detailHeader = Label("Title", detail.transform, "아이템 분석", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(detailHeader.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(16, -40), new Vector2(-16, -5));

            var preview = Image("Preview", detail.transform, new Color(.13f, .08f, .24f, .66f));
            Stretch(preview.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(18, -190), new Vector2(158, -50));
            AddOutline(preview.gameObject, new Color(.63f, .51f, 1f, .42f), 1);
            var glyph = Label("Glyph", preview.transform, "◇", 64, FontStyle.Bold,
                new Color(.57f, .76f, 1f), TextAnchor.MiddleCenter);
            Stretch(glyph.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8));

            var itemName = Label("ItemName", detail.transform, "아이템을 선택하세요", 22, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            Stretch(itemName.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(176, -104), new Vector2(-16, -52));
            var itemType = Label("ItemType", detail.transform, "NO ITEM SELECTED", 12, FontStyle.Bold,
                new Color(.63f, .72f, 1f), TextAnchor.MiddleLeft);
            Stretch(itemType.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(176, -139), new Vector2(-16, -105));
            var quantity = Label("ItemQuantity", detail.transform, string.Empty, 12, FontStyle.Normal,
                new Color(.61f, .76f, .80f), TextAnchor.MiddleLeft);
            Stretch(quantity.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(176, -176), new Vector2(-16, -140));
            var divider = Image("Divider", detail.transform, new Color(.68f, .56f, 1f, .32f));
            Stretch(divider.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(18, -211), new Vector2(-18, -209));
            var description = Label("Description", detail.transform,
                "슬롯을 클릭하면 아이템 정보가 표시됩니다.", 13, FontStyle.Normal,
                new Color(.76f, .86f, .88f), TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(description.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(18, 74), new Vector2(-18, -228));

            var use = MakeButton("UseButton", detail.transform, "사용", new Color(.48f, .31f, .82f, .84f), 14);
            Stretch(use.GetComponent<RectTransform>(), Vector2.zero, new Vector2(.5f, 0),
                new Vector2(18, 16), new Vector2(-6, 60));
            var drop = MakeButton("DropButton", detail.transform, "버리기", new Color(.17f, .11f, .29f, .78f), 14);
            Stretch(drop.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(1, 0),
                new Vector2(6, 16), new Vector2(-18, 60));
            var sort = MakeButton("SortButton", card.transform, "≡  자동 정렬", new Color(.22f, .14f, .38f, .78f), 14);
            Stretch(sort.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20, 20), new Vector2(176, 66));
            var hint = Label("Hint", card.transform, "클릭: 선택   •   드래그: 슬롯 이동   •   Tab: 닫기", 12,
                FontStyle.Normal, new Color(.61f, .76f, .80f), TextAnchor.MiddleLeft);
            Stretch(hint.rectTransform, Vector2.zero, new Vector2(1, 0),
                new Vector2(196, 20), new Vector2(-20, 66));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("maxInvSlot").intValue = SlotCount;
            serialized.FindProperty("invSlotPrefab").objectReferenceValue = slotPrefab;
            serialized.FindProperty("parentTrs").objectReferenceValue = content;
            serialized.FindProperty("itemNameText").objectReferenceValue = itemName;
            serialized.FindProperty("itemTypeText").objectReferenceValue = itemType;
            serialized.FindProperty("itemDescriptionText").objectReferenceValue = description;
            serialized.FindProperty("itemQuantityText").objectReferenceValue = quantity;
            serialized.FindProperty("itemGlyphText").objectReferenceValue = glyph;
            serialized.FindProperty("useButton").objectReferenceValue = use;
            serialized.FindProperty("dropButton").objectReferenceValue = drop;
            serialized.FindProperty("sortButton").objectReferenceValue = sort;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("editorLayoutVersion").intValue = LayoutVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var craftPanel = UnityEngine.Object.FindFirstObjectByType<CraftPanel>(FindObjectsInactive.Include);
            if (craftPanel) SurvivalUIInputBindingEditor.EnsureNavigationForPanels(panel, craftPanel);
            SurvivalUIRoundedStyle.ApplyToRoot(panel.gameObject);

            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            if (saveScene) EditorSceneManager.SaveScene(panel.gameObject.scene);
            Debug.Log("[Survival UI] Rebuilt translucent survival inventory with 40 serialized UGUI slots.");
        }

        private static void EnsureCraftController(InventoryPanel panel)
        {
            if (UnityEngine.Object.FindFirstObjectByType<CraftController>(FindObjectsInactive.Include)) return;

            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
            var host = inventory ? inventory.gameObject : panel.gameObject;
            var controller = Undo.AddComponent<CraftController>(host);
            var database = AssetDatabase.LoadAssetAtPath<BluePrintDataBase>(
                "Assets/003_Resources/Data/BluePrints.asset");
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("bpDB").objectReferenceValue = database;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureInteractionCanvas(GameObject target)
        {
            var canvas = target.GetComponent<Canvas>();
            if (!canvas) canvas = Undo.AddComponent<Canvas>(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvas.enabled = true;

            var scaler = target.GetComponent<CanvasScaler>();
            if (!scaler) scaler = Undo.AddComponent<CanvasScaler>(target);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;

            var raycaster = target.GetComponent<GraphicRaycaster>();
            if (!raycaster) raycaster = Undo.AddComponent<GraphicRaycaster>(target);
            raycaster.enabled = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            var group = target.GetComponent<CanvasGroup>();
            if (!group) group = Undo.AddComponent<CanvasGroup>(target);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static GameObject BuildSlotPrefab()
        {
            EnsureFolder("Assets/002_Prefabs/UI");
            var root = new GameObject("InventorySlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(ItemSlot), typeof(SurvivalUIInteractableFX));
            root.layer = 5;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(128, 104);
            var background = root.GetComponent<Image>();
            background.color = new Color(.12f, .075f, .22f, .68f);
            AddOutline(root, new Color(.56f, .46f, .92f, .32f), 1);

            var glyph = Label("Glyph", root.transform, "◇", 31, FontStyle.Bold,
                new Color(.54f, .64f, .88f, .62f), TextAnchor.MiddleCenter);
            Stretch(glyph.rectTransform, Vector2.zero, Vector2.one, new Vector2(4, 7), new Vector2(-4, -27));
            var itemName = Label("Name", root.transform, string.Empty, 11, FontStyle.Bold,
                Color.white, TextAnchor.LowerLeft);
            Stretch(itemName.rectTransform, Vector2.zero, Vector2.one, new Vector2(7, 5), new Vector2(-7, -63));
            var count = Label("Count", root.transform, string.Empty, 12, FontStyle.Bold,
                Color.white, TextAnchor.UpperRight);
            Stretch(count.rectTransform, new Vector2(.45f, .48f), Vector2.one, Vector2.zero, new Vector2(-7, -5));

            var slot = root.GetComponent<ItemSlot>();
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("background").objectReferenceValue = background;
            serialized.FindProperty("glyphText").objectReferenceValue = glyph;
            serialized.FindProperty("nameText").objectReferenceValue = itemName;
            serialized.FindProperty("countText").objectReferenceValue = count;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SurvivalUIRoundedStyle.ApplyToRoot(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
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

        private static Button MakeButton(string name, Transform parent, string text, Color color, int size)
        {
            var image = Image(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label("Label", image.transform, text, size, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 4), new Vector2(-6, -4));
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

        private static void AddOutline(GameObject go, Color color, float size)
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
