#if UNITY_EDITOR
using System.Collections.Generic;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Managers;
using _001_Scripts.UI;
using _001_Scripts.UI.Component;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalDataPanelsInstaller
    {
        private const string BlueprintAssetPath = "Assets/003_Resources/Data/BluePrints.asset";
        private const int LayoutVersion = 2;

        static SurvivalDataPanelsInstaller() => EditorApplication.delayCall += RebuildIfNeeded;

        private static void RebuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            var panel = UnityEngine.Object.FindFirstObjectByType<BlueprintPanel>(FindObjectsInactive.Include);
            if (panel)
            {
                var version = new SerializedObject(panel).FindProperty("editorLayoutVersion");
                if (version != null && version.intValue >= LayoutVersion) return;
            }
            Rebuild();
        }

        [MenuItem("Tools/Survival UI/Rebuild Scene-Authored Data Panels")]
        private static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var manager = UnityEngine.Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            var logs = UnityEngine.Object.FindFirstObjectByType<LogPanel>(FindObjectsInactive.Include);
            if (!manager || !inventory || !logs)
            {
                Debug.LogError("[Survival UI] UIManager, InventoryPanel, and LogPanel must exist in the active scene.");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<BluePrintDataBase>(BlueprintAssetPath);
            if (!database)
            {
                Debug.LogError("[Survival UI] BluePrintDataBase asset is missing.");
                return;
            }
            database.Reload();

            Undo.RegisterFullObjectHierarchyUndo(inventory.gameObject, "Rebuild Scene Authored Data UI");
            Undo.RegisterFullObjectHierarchyUndo(logs.gameObject, "Rebuild Scene Authored Data UI");
            ConfigureInventory(inventory);
            BuildLogPanel(logs);
            var blueprint = BuildBlueprintPanel(manager, database);
            manager.uiPanels["Log"] = logs;
            manager.uiPanels["Blueprint"] = blueprint;

            AddParticlePool(inventory.gameObject);
            AddParticlePool(logs.gameObject);
            AddParticlePool(blueprint.gameObject);
            AddEffects(inventory.gameObject);
            AddEffects(logs.gameObject);
            AddEffects(blueprint.gameObject);
            foreach (var radial in UnityEngine.Object.FindObjectsByType<SimpleRadialMenuView>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                AddParticlePool(radial.gameObject);

            SurvivalUIRoundedStyle.ApplyToRoot(inventory.gameObject);
            SurvivalUIRoundedStyle.ApplyToRoot(logs.gameObject);
            SurvivalUIRoundedStyle.ApplyToRoot(blueprint.gameObject);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(logs);
            EditorUtility.SetDirty(blueprint);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Survival UI] Rebuilt Inventory, Log, and Blueprint as editable scene-authored panels.");
        }

        private static void ConfigureInventory(InventoryPanel panel)
        {
            var header = Find(panel.transform, "Header");
            if (!header) return;
            DestroyNamed(panel.transform, "TopNavigation");
            DestroyNamed(panel.transform, "UnifiedTabs");
            var capacity = header.Find("Capacity");
            if (capacity) capacity.gameObject.SetActive(false);
            var oldClose = header.Find("CloseButton");
            if (oldClose) Undo.DestroyObjectImmediate(oldClose.gameObject);
            var navigation = BuildNavigation(header, "Inventory");
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("modalNavigation").objectReferenceValue = navigation;
            serialized.FindProperty("inventoryTabButton").objectReferenceValue = null;
            serialized.FindProperty("craftTabButton").objectReferenceValue = null;
            serialized.FindProperty("closeButton").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var root = Find(panel.transform, "UGUI_InventoryRoot")?.GetComponent<Image>();
            if (root) root.color = new Color(.012f, .006f, .035f, .94f);
            var card = Find(panel.transform, "InventoryCard")?.GetComponent<Image>();
            if (card) card.color = new Color(.04f, .02f, .09f, .96f);
        }

        private static void BuildLogPanel(LogPanel panel)
        {
            ClearChildren(panel.transform);
            ConfigureCanvas(panel.gameObject);
            var frame = BuildFrame(panel.transform, "Log", "수집 로그", "월드에서 발견한 기록이 이곳에 보관됩니다.");
            var left = Image("LogList", frame.Card, new Color(.045f, .025f, .10f, .78f));
            SetRect(left.rectTransform, new Vector2(0f, 1f), new Vector2(390f, 570f), new Vector2(20f, -96f),
                new Vector2(0f, 1f));
            Outline(left.gameObject);
            var content = Rect("Content", left.transform);
            Stretch(content, 12f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = layout.childControlWidth = false;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            var views = new List<LogEntryView>();
            for (int i = 0; i < 24; i++)
            {
                var button = Button($"LogSlot_{i:00}", content, "로그 슬롯", new Color(.13f, .08f, .23f, .88f));
                ((RectTransform)button.transform).sizeDelta = new Vector2(366f, 54f);
                var view = button.gameObject.AddComponent<LogEntryView>();
                var so = new SerializedObject(view);
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("title").objectReferenceValue = button.GetComponentInChildren<Text>();
                so.ApplyModifiedPropertiesWithoutUndo();
                views.Add(view);
            }
            var empty = Text("Empty", left.transform, "아직 수집한 로그가 없습니다.", 16, TextAnchor.MiddleCenter);
            Stretch(empty.rectTransform, 16f);

            var detail = Image("LogDetails", frame.Card, new Color(.055f, .03f, .12f, .78f));
            SetRect(detail.rectTransform, new Vector2(1f, 1f), new Vector2(770f, 570f), new Vector2(-20f, -96f),
                new Vector2(1f, 1f));
            Outline(detail.gameObject);
            var preview = Image("LogImage", detail.transform, new Color(.13f, .08f, .24f, .66f));
            SetRect(preview.rectTransform, new Vector2(0f, 1f), new Vector2(280f, 280f), new Vector2(30f, -30f),
                new Vector2(0f, 1f));
            preview.preserveAspect = true;
            Outline(preview.gameObject);
            var title = Text("LogTitle", detail.transform, "로그를 선택하세요", 26, TextAnchor.UpperLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(410f, 90f), new Vector2(340f, -35f),
                new Vector2(0f, 1f));
            var body = Text("LogBody", detail.transform, string.Empty, 17, TextAnchor.UpperLeft);
            SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(700f, 190f), new Vector2(32f, -330f),
                new Vector2(0f, 1f));
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("navigation").objectReferenceValue = frame.Navigation;
            AssignList(serialized.FindProperty("entryViews"), views);
            serialized.FindProperty("detailTitle").objectReferenceValue = title;
            serialized.FindProperty("detailBody").objectReferenceValue = body;
            serialized.FindProperty("emptyLabel").objectReferenceValue = empty;
            serialized.FindProperty("detailImage").objectReferenceValue = preview;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BlueprintPanel BuildBlueprintPanel(UIManager manager, BluePrintDataBase database)
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<BlueprintPanel>(FindObjectsInactive.Include);
            GameObject go;
            if (existing)
            {
                go = existing.gameObject;
                ClearChildren(go.transform);
            }
            else
            {
                go = new GameObject("BlueprintPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(BlueprintPanel));
                go.transform.SetParent(manager.transform, false);
            }
            ConfigureCanvas(go);
            var panel = go.GetComponent<BlueprintPanel>();
            var frame = BuildFrame(go.transform, "Blueprint", "청사진", "BLUEPRINT DATABASE");
            var bay = Image("BlueprintBay", frame.Card, new Color(.045f, .025f, .10f, .66f));
            SetRect(bay.rectTransform, new Vector2(.5f, 1f), new Vector2(1180f, 570f), new Vector2(0f, -96f),
                new Vector2(.5f, 1f));
            Outline(bay.gameObject);
            var viewport = Image("Viewport", bay.transform, new Color(0f, 0f, 0f, .08f));
            Stretch(viewport.rectTransform, 14f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewport.rectTransform;
            var content = Rect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 14f;
            vertical.childControlHeight = vertical.childControlWidth = false;
            vertical.childForceExpandHeight = vertical.childForceExpandWidth = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            var grouped = new Dictionary<string, List<_001_Scripts.Data.BluePrint.BluePrint>>();
            var order = new List<string>();
            foreach (var blueprint in database.GetAllBluePrints())
            {
                if (!grouped.TryGetValue(blueprint.categoryPath, out var list))
                {
                    list = new List<_001_Scripts.Data.BluePrint.BluePrint>();
                    grouped.Add(blueprint.categoryPath, list);
                    order.Add(blueprint.categoryPath);
                }
                list.Add(blueprint);
            }
            var tiles = new List<BlueprintTileView>();
            foreach (string category in order)
            {
                var entries = grouped[category];
                int rows = Mathf.Max(1, Mathf.CeilToInt(entries.Count / 6f));
                var section = Image("Category_" + category.Replace('/', '_'), content,
                    new Color(.035f, .025f, .08f, .54f));
                section.rectTransform.sizeDelta = new Vector2(1138f, 48f + rows * 154f);
                var bar = Image("CategoryBar", section.transform, new Color(.20f, .34f, .62f, .66f));
                SetRect(bar.rectTransform, new Vector2(.5f, 1f), new Vector2(1110f, 34f), new Vector2(0f, -7f),
                    new Vector2(.5f, 1f));
                Outline(bar.gameObject);
                var categoryLabel = Text("Label", bar.transform, category, 15, TextAnchor.MiddleLeft);
                Stretch(categoryLabel.rectTransform, 14f);
                var grid = Rect("Grid", section.transform);
                grid.anchorMin = Vector2.zero;
                grid.anchorMax = Vector2.one;
                grid.offsetMin = new Vector2(14f, 4f);
                grid.offsetMax = new Vector2(-14f, -48f);
                var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(172f, 146f);
                gridLayout.spacing = new Vector2(12f, 8f);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 6;
                foreach (var blueprint in entries) tiles.Add(BuildBlueprintTile(grid, blueprint));
            }
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("summary").objectReferenceValue = frame.Subtitle;
            serialized.FindProperty("navigation").objectReferenceValue = frame.Navigation;
            AssignList(serialized.FindProperty("tiles"), tiles);
            serialized.FindProperty("editorLayoutVersion").intValue = LayoutVersion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static BlueprintTileView BuildBlueprintTile(Transform parent,
            _001_Scripts.Data.BluePrint.BluePrint blueprint)
        {
            var tileImage = Image("Blueprint_" + blueprint.bluePrintId, parent, new Color(0f, 0f, 0f, .01f));
            tileImage.gameObject.AddComponent<Button>();
            var view = tileImage.gameObject.AddComponent<BlueprintTileView>();
            var disc = Image("Disc", tileImage.transform, new Color(.16f, .43f, .72f, .92f));
            SetRect(disc.rectTransform, new Vector2(.5f, 1f), new Vector2(88f, 88f), new Vector2(0f, -4f),
                new Vector2(.5f, 1f));
            var glyph = Text("Glyph", disc.transform, "◇", 34, TextAnchor.MiddleCenter);
            Stretch(glyph.rectTransform, 8f);
            var icon = Image("Icon", disc.transform, Color.white);
            Stretch(icon.rectTransform, 13f);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            var name = Text("Name", tileImage.transform, blueprint.bluePrintName, 13, TextAnchor.UpperCenter);
            SetRect(name.rectTransform, new Vector2(.5f, 1f), new Vector2(168f, 38f), new Vector2(0f, -94f),
                new Vector2(.5f, 1f));
            var track = Image("ProgressTrack", tileImage.transform, new Color(.05f, .04f, .09f, .95f));
            SetRect(track.rectTransform, new Vector2(.5f, 0f), new Vector2(112f, 6f), new Vector2(0f, 17f),
                new Vector2(.5f, 0f));
            var fill = Image("Fill", track.transform, new Color(.48f, .78f, 1f, .95f));
            Stretch(fill.rectTransform);
            var progress = Text("Progress", tileImage.transform,
                $"( {blueprint.unlockProgress} / {blueprint.unlockRequired} )", 12, TextAnchor.MiddleCenter);
            SetRect(progress.rectTransform, new Vector2(.5f, 0f), new Vector2(120f, 22f), Vector2.zero,
                new Vector2(.5f, 0f));
            var serialized = new SerializedObject(view);
            serialized.FindProperty("blueprintId").intValue = blueprint.bluePrintId;
            serialized.FindProperty("nameLabel").objectReferenceValue = name;
            serialized.FindProperty("glyphLabel").objectReferenceValue = glyph;
            serialized.FindProperty("progressLabel").objectReferenceValue = progress;
            serialized.FindProperty("disc").objectReferenceValue = disc;
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("progressFill").objectReferenceValue = fill.rectTransform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static (Transform Card, ModalNavigation Navigation, Text Subtitle) BuildFrame(Transform parent,
            string key, string title, string subtitle)
        {
            var root = Image(key + "Root", parent, new Color(.012f, .006f, .035f, .94f));
            Stretch(root.rectTransform);
            var card = Image(key + "Card", root.transform, new Color(.04f, .02f, .09f, .96f));
            SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(1220f, 700f), new Vector2(0f, -24f));
            Outline(card.gameObject);
            var header = Image("Header", card.transform, new Color(.11f, .055f, .22f, .64f));
            SetRect(header.rectTransform, new Vector2(.5f, 1f), new Vector2(1220f, 78f), Vector2.zero,
                new Vector2(.5f, 1f));
            var accent = Image("Accent", header.transform, new Color(.52f, .72f, 1f, .9f));
            SetRect(accent.rectTransform, new Vector2(0f, .5f), new Vector2(5f, 78f), Vector2.zero,
                new Vector2(0f, .5f));
            var heading = Text("Title", header.transform, title, 28, TextAnchor.MiddleLeft);
            SetRect(heading.rectTransform, new Vector2(0f, .5f), new Vector2(420f, 38f), new Vector2(32f, 10f),
                new Vector2(0f, .5f));
            var sub = Text("Subtitle", header.transform, subtitle, 12, TextAnchor.MiddleLeft);
            SetRect(sub.rectTransform, new Vector2(0f, .5f), new Vector2(520f, 24f), new Vector2(33f, -18f),
                new Vector2(0f, .5f));
            return (card.transform, BuildNavigation(header.transform, key), sub);
        }

        private static ModalNavigation BuildNavigation(Transform header, string key)
        {
            var nav = Rect("ModalNavigation", header);
            nav.anchorMin = nav.anchorMax = new Vector2(1f, .5f);
            nav.pivot = new Vector2(1f, .5f);
            nav.sizeDelta = new Vector2(550f, 48f);
            nav.anchoredPosition = new Vector2(-18f, 0f);
            var component = nav.gameObject.AddComponent<ModalNavigation>();
            var inventory = NavButton(nav, "InventoryButton", "▦  인벤토리", 0f, key == "Inventory");
            var logs = NavButton(nav, "LogButton", "▤  로그", 142f, key == "Log");
            var blueprint = NavButton(nav, "BlueprintButton", "◇  청사진", 284f, key == "Blueprint");
            var close = NavButton(nav, "CloseButton", "×", 438f, false, 54f);
            var serialized = new SerializedObject(component);
            serialized.FindProperty("panelKey").stringValue = key;
            serialized.FindProperty("inventoryButton").objectReferenceValue = inventory;
            serialized.FindProperty("logButton").objectReferenceValue = logs;
            serialized.FindProperty("blueprintButton").objectReferenceValue = blueprint;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static Button NavButton(Transform parent, string name, string label, float x, bool active,
            float width = 136f)
        {
            var button = Button(name, parent, label,
                active ? new Color(.30f, .36f, .78f, .96f) : new Color(.09f, .08f, .19f, .82f));
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.pivot = new Vector2(0f, .5f);
            rect.sizeDelta = new Vector2(width, 42f);
            rect.anchoredPosition = new Vector2(x, 0f);
            button.interactable = !active;
            return button;
        }

        private static void AddParticlePool(GameObject panel)
        {
            var existing = panel.GetComponent<UIInteractionParticlePool>();
            if (existing) Undo.DestroyObjectImmediate(existing);
            var old = panel.transform.Find("InteractionParticles");
            if (old) Undo.DestroyObjectImmediate(old.gameObject);
            var pool = Undo.AddComponent<UIInteractionParticlePool>(panel);
            var root = Rect("InteractionParticles", panel.transform);
            Stretch(root);
            root.SetAsLastSibling();
            var particles = new List<Image>();
            for (int i = 0; i < 72; i++)
            {
                var particle = Image($"Spark_{i:00}", root, Color.clear);
                particle.raycastTarget = false;
                particle.gameObject.SetActive(false);
                particles.Add(particle);
            }
            var serialized = new SerializedObject(pool);
            AssignList(serialized.FindProperty("particles"), particles);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddEffects(GameObject root)
        {
            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
                if (!selectable.GetComponent<SurvivalUIInteractableFX>())
                    Undo.AddComponent<SurvivalUIInteractableFX>(selectable.gameObject);
            foreach (var slot in root.GetComponentsInChildren<ItemSlot>(true))
                if (!slot.GetComponent<SurvivalUIInteractableFX>())
                    Undo.AddComponent<SurvivalUIInteractableFX>(slot.gameObject);
        }

        private static void ConfigureCanvas(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            Stretch(rect);
            var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 120;
            var scaler = go.GetComponent<CanvasScaler>() ?? go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = .5f;
            if (!go.GetComponent<GraphicRaycaster>()) go.AddComponent<GraphicRaycaster>();
            var group = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Text(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Color color)
        {
            var image = Image(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text("Label", image.transform, label, 14, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 6f);
            return button;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Outline(GameObject go)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(.62f, .49f, 1f, .42f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position,
            Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static void DestroyNamed(Transform root, string name)
        {
            var target = Find(root, name);
            if (target) Undo.DestroyObjectImmediate(target.gameObject);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
        }

        private static void AssignList<T>(SerializedProperty property, IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
