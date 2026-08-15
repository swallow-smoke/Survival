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
        private const int LayoutVersion = 7;

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

            var root = Image(RootName, panel.transform, new Color(.012f, .006f, .035f, .94f));
            Stretch(root.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = Image("InventoryCard", root.transform, new Color(.04f, .02f, .09f, .96f));
            Anchor(card.rectTransform, new Vector2(.5f, .5f), new Vector2(1500, 760), new Vector2(0, -24));
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
            var equipmentArea = Image("PlayerEquipment", card.transform, new Color(.055f, .03f, .12f, .62f));
            Stretch(equipmentArea.rectTransform, new Vector2(1, 0), Vector2.one,
                new Vector2(-418, 88), new Vector2(-20, -94));
            AddOutline(equipmentArea.gameObject, new Color(.55f, .48f, .91f, .34f), 1);
            var equipmentHeader = Label("Title", equipmentArea.transform, "플레이어 장비", 15,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Stretch(equipmentHeader.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(16, -42), new Vector2(-12, -5));
            var equipmentMeta = Label("Meta", equipmentArea.transform, "DRAG TO EQUIP", 9,
                FontStyle.Bold, new Color(.65f, .58f, .82f), TextAnchor.MiddleRight);
            Stretch(equipmentMeta.rectTransform, new Vector2(.45f, 1), Vector2.one,
                new Vector2(0, -42), new Vector2(-14, -5));

            var equipmentContent = CreateRect("EquipmentSlots", equipmentArea.transform);
            Stretch(equipmentContent, Vector2.zero, Vector2.one, new Vector2(8, 12), new Vector2(-8, -52));
            var equipmentGrid = equipmentContent.gameObject.AddComponent<GridLayoutGroup>();
            equipmentGrid.padding = new RectOffset(2, 2, 2, 2);
            equipmentGrid.cellSize = new Vector2(174, 104);
            equipmentGrid.spacing = new Vector2(10, 10);
            equipmentGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            equipmentGrid.constraintCount = 2;
            equipmentGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            equipmentGrid.childAlignment = TextAnchor.UpperCenter;

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
            string[] equipmentNames = { "머리", "몸체", "다리", "발", "강화 칩 1", "강화 칩 2", "강화 칩 3", "강화 칩 4" };
            string[] equipmentGlyphs = { "◉", "◇", "Ⅱ", "⌞", "⬡", "⬡", "⬡", "⬡" };
            for (int i = 0; i < equipmentNames.Length; i++)
            {
                var equipmentSlotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, equipmentContent);
                equipmentSlotObject.name = $"EquipmentSlot_{i:00}_{equipmentNames[i]}";
                var equipmentSlot = equipmentSlotObject.GetComponent<ItemSlot>();
                equipmentSlot.ConfigurePlaceholder(equipmentGlyphs[i], equipmentNames[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(equipmentSlot);
            }
            for (int i = 0; i < SlotCount; i++)
            {
                var slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, content);
                slot.name = $"Slot_{i:00}";
            }

            var sort = MakeButton("SortButton", card.transform, "≡  자동 정렬", new Color(.22f, .14f, .38f, .78f), 14);
            Stretch(sort.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
                new Vector2(20, 20), new Vector2(176, 66));
            var hint = Label("Hint", card.transform, "호버: 정보   •   장비 좌클릭: 장착/해제   •   드래그: 슬롯 이동", 12,
                FontStyle.Normal, new Color(.61f, .76f, .80f), TextAnchor.MiddleLeft);
            Stretch(hint.rectTransform, Vector2.zero, new Vector2(1, 0),
                new Vector2(196, 20), new Vector2(-20, 66));

            var tooltip = Image("ItemTooltip", root.transform, new Color(.065f, .035f, .14f, .96f));
            tooltip.rectTransform.anchorMin = tooltip.rectTransform.anchorMax = Vector2.zero;
            tooltip.rectTransform.pivot = new Vector2(0, 1);
            tooltip.rectTransform.sizeDelta = new Vector2(340, 190);
            tooltip.rectTransform.anchoredPosition = new Vector2(24, 220);
            AddOutline(tooltip.gameObject, new Color(.68f, .55f, 1f, .72f), 2);
            var tooltipName = Label("Name", tooltip.transform, string.Empty, 18, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft);
            Stretch(tooltipName.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(16, -38), new Vector2(-16, -6));
            var tooltipType = Label("Type", tooltip.transform, string.Empty, 10, FontStyle.Bold,
                new Color(.65f, .75f, 1f), TextAnchor.MiddleLeft);
            Stretch(tooltipType.rectTransform, new Vector2(0, 1), Vector2.one,
                new Vector2(16, -62), new Vector2(-16, -38));
            var tooltipDescription = Label("Description", tooltip.transform, string.Empty, 12, FontStyle.Normal,
                new Color(.82f, .86f, .95f), TextAnchor.UpperLeft);
            tooltipDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipDescription.verticalOverflow = VerticalWrapMode.Truncate;
            Stretch(tooltipDescription.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(16, 40), new Vector2(-16, -70));
            var tooltipMeta = Label("Meta", tooltip.transform, string.Empty, 11, FontStyle.Bold,
                new Color(.76f, .64f, 1f), TextAnchor.MiddleLeft);
            Stretch(tooltipMeta.rectTransform, Vector2.zero, new Vector2(1, 0),
                new Vector2(16, 8), new Vector2(-16, 36));
            tooltip.gameObject.SetActive(false);

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("maxInvSlot").intValue = SlotCount;
            serialized.FindProperty("invSlotPrefab").objectReferenceValue = slotPrefab;
            serialized.FindProperty("parentTrs").objectReferenceValue = content;
            serialized.FindProperty("equipmentRoot").objectReferenceValue = equipmentContent;
            serialized.FindProperty("itemNameText").objectReferenceValue = null;
            serialized.FindProperty("itemTypeText").objectReferenceValue = null;
            serialized.FindProperty("itemDescriptionText").objectReferenceValue = null;
            serialized.FindProperty("itemQuantityText").objectReferenceValue = null;
            serialized.FindProperty("itemGlyphText").objectReferenceValue = null;
            serialized.FindProperty("useButton").objectReferenceValue = null;
            serialized.FindProperty("dropButton").objectReferenceValue = null;
            serialized.FindProperty("tooltipRoot").objectReferenceValue = tooltip.rectTransform;
            serialized.FindProperty("tooltipNameText").objectReferenceValue = tooltipName;
            serialized.FindProperty("tooltipTypeText").objectReferenceValue = tooltipType;
            serialized.FindProperty("tooltipDescriptionText").objectReferenceValue = tooltipDescription;
            serialized.FindProperty("tooltipMetaText").objectReferenceValue = tooltipMeta;
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
            Debug.Log("[Survival UI] Rebuilt inventory with 40 storage slots and 8 player equipment slots.");
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
            canvas.sortingOrder = 120;
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
