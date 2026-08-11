using System;
using System.Collections;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.Type.Item;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using BlueprintModel = _001_Scripts.Data.BluePrint.BluePrint;

namespace _001_Scripts.UI
{
    /// <summary>
    /// Compact Subnautica-style fabrication tree: only circular category and recipe nodes
    /// radiate from a central hub. No full-screen card or inventory-style details window.
    /// </summary>
    public sealed class WorkbenchPanel : PanelBase
    {
        public const int CurrentVisualVersion = 3;
        private const string VisualRootName = "WorkbenchRadialRoot";
        private static readonly Color Cyan = new(.08f, .9f, 1f, 1f);
        private static readonly Color CyanDark = new(.018f, .19f, .23f, .94f);
        private static readonly Color Ready = new(.16f, .76f, .84f, .96f);
        private static readonly Color Missing = new(.28f, .30f, .32f, .92f);

        [Header("Data")]
        [SerializeField] private BluePrintDataBase blueprintDatabase;
        [SerializeField] private ItemDataBase itemDatabase;

        [Header("Radial Layout")]
        [SerializeField] private float categoryRadius = 142f;
        [SerializeField] private float recipeRadius = 300f;
        [SerializeField] private float categoryNodeSize = 92f;
        [SerializeField] private float recipeNodeSize = 106f;

        [Header("Motion")]
        [SerializeField, Range(.1f, .7f)] private float openDuration = .28f;
        [SerializeField, Range(.05f, .4f)] private float closeDuration = .15f;
        [SerializeField, Range(0f, .12f)] private float nodeDelay = .04f;
        [SerializeField, HideInInspector] private int visualVersion;

        public int VisualVersion => visualVersion;

        private CanvasGroup _group;
        private RectTransform _radialRoot;
        private Transform _connectorRoot;
        private Transform _categoryRoot;
        private Transform _recipeRoot;
        private Text _caption;
        private Text _status;
        private Button _hubButton;

        private readonly List<GameObject> _categoryObjects = new();
        private readonly List<GameObject> _recipeObjects = new();
        private readonly List<RadialNode> _animatedNodes = new();
        private readonly List<ItemType> _categories = new();
        private IDisposable _subscriptions;
        private IInventoryReader _inventory;
        private IPublisher<CraftReqMessage> _craftPublisher;
        private IPublisher<UIReqMessage> _uiPublisher;
        private ItemType _selectedCategory;
        private Coroutine _motion;
        private float _lastCraftTime = -10f;
        private bool _wired;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void UpgradeSceneVisualToRadial()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (!scene.IsValid() || scene.path != "Assets/000_Scenes/SampleScene.unity") return;
                var panel = FindAnyObjectByType<WorkbenchPanel>(FindObjectsInactive.Include);
                if (!panel || panel.VisualVersion == CurrentVisualVersion && panel.transform.Find(VisualRootName)) return;
                panel.RebuildVisualTreeForEditor();
                var manager = FindAnyObjectByType<_001_Scripts.Managers.UIManager>(FindObjectsInactive.Include);
                if (manager)
                {
                    manager.uiPanels["Workbench"] = panel;
                    UnityEditor.EditorUtility.SetDirty(manager);
                }
                UnityEditor.EditorUtility.SetDirty(panel);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log("[Workbench] Rebuilt UI as compact radial recipe nodes.");
            };
        }
#endif

        private sealed class RadialNode
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 Target;
        }

        private new void Awake()
        {
            base.Awake();
            EnsureVisualTree();
            SetHiddenImmediate();
        }

        private void Start()
        {
            EnsureVisualTree();
            WireHub();
        }

        public override void Open()
        {
            EnsureVisualTree();
            EnsureData();
            WireHub();
            BuildCategories();

            if (_motion != null) StopCoroutine(_motion);
            isViz = true;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _motion = StartCoroutine(AnimateOpen());
        }

        public override void Close()
        {
            EnsureVisualTree();
            if (_motion != null) StopCoroutine(_motion);
            isViz = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _motion = StartCoroutine(AnimateClose());
        }

        public void RebuildVisualTreeForEditor()
        {
            _group = GetComponent<CanvasGroup>();
            var existing = transform.Find(VisualRootName);
            if (existing)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            _wired = false;
            BuildVisualTree();
            SetHiddenImmediate();
        }

        [Inject]
        private void Construct(IPublisher<CraftReqMessage> craftPublisher,
            IPublisher<UIReqMessage> uiPublisher,
            ISubscriber<CraftResultMessage> craftResults,
            ISubscriber<InvChangedMessage> inventoryChanges,
            IInventoryReader inventory)
        {
            _subscriptions?.Dispose();
            _craftPublisher = craftPublisher;
            _uiPublisher = uiPublisher;
            _inventory = inventory;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(craftResults.Subscribe(OnCraftResult));
            builder.Add(inventoryChanges.Subscribe(_ => RefreshRecipeStates()));
            _subscriptions = builder.Build();
        }

        private void BuildCategories()
        {
            ClearObjects(_categoryObjects);
            ClearObjects(_recipeObjects);
            ClearConnectors();
            _animatedNodes.Clear();
            _categories.Clear();

            if (!blueprintDatabase || !itemDatabase)
            {
                SetStatus("제작 데이터를 찾을 수 없습니다", false);
                return;
            }

            var blueprints = blueprintDatabase.GetAllBluePrints();
            for (int i = 0; i < blueprints.Count; i++)
            {
                if (!blueprints[i].isUnlocked || !TryGetResult(blueprints[i], out var result)) continue;
                if (!_categories.Contains(result.itemType)) _categories.Add(result.itemType);
            }

            if (_categories.Count == 0)
            {
                _caption.text = "발견된 설계도 없음";
                SetStatus("탐사하여 설계도를 발견하세요", false);
                return;
            }

            for (int i = 0; i < _categories.Count; i++)
            {
                ItemType category = _categories[i];
                float angle = GetCategoryAngle(i, _categories.Count);
                Vector2 position = Direction(angle) * categoryRadius;
                CreateConnector(Vector2.zero, position, .28f);
                var button = CreateCircleButton($"Category_{category}", _categoryRoot, categoryNodeSize,
                    CategoryGlyph(category), CategoryLabel(category), CyanDark);
                var rect = button.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                var group = button.gameObject.AddComponent<CanvasGroup>();
                button.onClick.AddListener(() => SelectCategory(category));
                _categoryObjects.Add(button.gameObject);
                _animatedNodes.Add(new RadialNode { Rect = rect, Group = group, Target = position });
            }

            SelectCategory(_categories.Contains(_selectedCategory) ? _selectedCategory : _categories[0]);
        }

        private void SelectCategory(ItemType category)
        {
            _selectedCategory = category;
            _caption.text = CategoryLabel(category);
            BuildRecipes(category);
        }

        private void BuildRecipes(ItemType category)
        {
            for (int i = _animatedNodes.Count - 1; i >= 0; i--)
            {
                if (_animatedNodes[i].Rect && _animatedNodes[i].Rect.parent == _recipeRoot)
                    _animatedNodes.RemoveAt(i);
            }
            ClearObjects(_recipeObjects);
            ClearRecipeConnectors();

            if (!blueprintDatabase || !itemDatabase) return;
            var matches = new List<BlueprintModel>();
            var blueprints = blueprintDatabase.GetAllBluePrints();
            for (int i = 0; i < blueprints.Count; i++)
            {
                if (!blueprints[i].isUnlocked || !TryGetResult(blueprints[i], out var result) ||
                    result.itemType != category) continue;
                matches.Add(blueprints[i]);
            }

            int categoryIndex = Mathf.Max(0, _categories.IndexOf(category));
            float centerAngle = GetCategoryAngle(categoryIndex, Mathf.Max(1, _categories.Count));
            for (int i = 0; i < matches.Count; i++)
            {
                BlueprintModel blueprint = matches[i];
                TryGetResult(blueprint, out var result);
                float spread = matches.Count <= 1 ? 0f : Mathf.Lerp(-34f, 34f, i / (matches.Count - 1f));
                Vector2 position = Direction(centerAngle + spread) * recipeRadius;
                Vector2 categoryPosition = Direction(centerAngle) * categoryRadius;
                CreateConnector(categoryPosition, position, .52f, true);

                bool affordable = CanAfford(blueprint);
                var button = CreateCircleButton($"Recipe_{blueprint.bluePrintId}", _recipeRoot, recipeNodeSize,
                    InventoryPanel.GetGlyph(result.itemType), DisplayName(blueprint, result.itemName),
                    affordable ? Ready : Missing);
                var rect = button.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                var group = button.gameObject.AddComponent<CanvasGroup>();
                button.interactable = affordable;
                AddIngredientHint(button.transform, blueprint);
                button.onClick.AddListener(() => Craft(blueprint));
                _recipeObjects.Add(button.gameObject);
                _animatedNodes.Add(new RadialNode { Rect = rect, Group = group, Target = position });
            }

            SetStatus(matches.Count == 0 ? "이 분류에는 제작 가능한 레시피가 없습니다" : "원형 레시피를 눌러 제작", matches.Count > 0);
            if (isViz && _motion == null) StartCoroutine(AnimateRecipeBranch());
        }

        private void Craft(BlueprintModel blueprint)
        {
            if (blueprint == null || !CanAfford(blueprint) || _craftPublisher == null) return;
            if (Time.unscaledTime - _lastCraftTime < .25f) return;
            _lastCraftTime = Time.unscaledTime;
            SetStatus("제작 중…", true);
            _craftPublisher.Publish(new CraftReqMessage(blueprint.bluePrintName));
        }

        private void OnCraftResult(CraftResultMessage result)
        {
            bool success = result.msgType == CraftMessageType.Success;
            SetStatus(success ? "제작 완료" : "재료 부족", success);
            RefreshRecipeStates();
        }

        private void RefreshRecipeStates()
        {
            if (isViz) BuildRecipes(_selectedCategory);
        }

        private void RequestClose()
        {
            if (_uiPublisher != null) _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Workbench"));
            else Close();
        }

        private IEnumerator AnimateOpen()
        {
            _group.alpha = 0f;
            _radialRoot.localScale = Vector3.one * .72f;
            PrepareNodesForAnimation(_animatedNodes);
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth01(elapsed / openDuration);
                _group.alpha = t;
                _radialRoot.localScale = Vector3.one * Mathf.LerpUnclamped(.72f, 1f, t);
                AnimateNodes(_animatedNodes, elapsed, openDuration);
                yield return null;
            }
            FinishNodes(_animatedNodes);
            _group.alpha = 1f;
            _radialRoot.localScale = Vector3.one;
            _motion = null;
        }

        private IEnumerator AnimateRecipeBranch()
        {
            var nodes = new List<RadialNode>();
            for (int i = 0; i < _animatedNodes.Count; i++)
                if (_recipeObjects.Contains(_animatedNodes[i].Rect.gameObject)) nodes.Add(_animatedNodes[i]);
            PrepareNodesForAnimation(nodes);
            float duration = Mathf.Max(.16f, openDuration * .75f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateNodes(nodes, elapsed, duration);
                yield return null;
            }
            FinishNodes(nodes);
        }

        private IEnumerator AnimateClose()
        {
            float startAlpha = _group.alpha;
            Vector3 startScale = _radialRoot.localScale;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Smooth01(elapsed / closeDuration);
                _group.alpha = Mathf.Lerp(startAlpha, 0f, t);
                _radialRoot.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * .74f, t);
                yield return null;
            }
            SetHiddenImmediate();
            _motion = null;
        }

        private void EnsureData()
        {
            if (!blueprintDatabase) blueprintDatabase = Resources.Load<BluePrintDataBase>("Data/BluePrints");
            if (!itemDatabase) itemDatabase = Resources.Load<ItemDataBase>("Data/ItemDataBase");
        }

        private void EnsureVisualTree()
        {
            _group = GetComponent<CanvasGroup>();
            if (!_group) _group = gameObject.AddComponent<CanvasGroup>();
            var root = transform.Find(VisualRootName);
            if (!root || visualVersion != CurrentVisualVersion) RebuildVisualTreeForEditor();
            else BindVisualTree(root);
        }

        private void BuildVisualTree()
        {
            var root = CreateRect(VisualRootName, transform);
            Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _radialRoot = CreateRect("RadialTree", root);
            Anchor(_radialRoot, new Vector2(.5f, .5f), new Vector2(760, 760), Vector2.zero);
            _connectorRoot = CreateRect("Connectors", _radialRoot);
            Stretch((RectTransform)_connectorRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _categoryRoot = CreateRect("Categories", _radialRoot);
            Stretch((RectTransform)_categoryRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _recipeRoot = CreateRect("Recipes", _radialRoot);
            Stretch((RectTransform)_recipeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _hubButton = CreateCircleButton("Hub", _radialRoot, 116f, "⌁", "제작대", CyanDark);
            _hubButton.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            AddOutline(_hubButton.gameObject, new Color(Cyan.r, Cyan.g, Cyan.b, .7f), 2f);

            _caption = Text("Caption", _radialRoot, "제작대", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Anchor(_caption.rectTransform, new Vector2(.5f, .5f), new Vector2(280, 30), new Vector2(0, -92));
            _status = Text("Status", _radialRoot, "원형 메뉴를 준비하는 중…", 12, FontStyle.Bold,
                new Color(.65f, .88f, .91f), TextAnchor.MiddleCenter);
            Anchor(_status.rectTransform, new Vector2(.5f, .5f), new Vector2(360, 28), new Vector2(0, -122));
            visualVersion = CurrentVisualVersion;
            WireHub();
        }

        private void BindVisualTree(Transform root)
        {
            _radialRoot = root.Find("RadialTree") as RectTransform;
            _connectorRoot = _radialRoot.Find("Connectors");
            _categoryRoot = _radialRoot.Find("Categories");
            _recipeRoot = _radialRoot.Find("Recipes");
            _hubButton = _radialRoot.Find("Hub").GetComponent<Button>();
            _caption = _radialRoot.Find("Caption").GetComponent<Text>();
            _status = _radialRoot.Find("Status").GetComponent<Text>();
        }

        private void WireHub()
        {
            if (_wired || !_hubButton) return;
            _hubButton.onClick.AddListener(RequestClose);
            _wired = true;
        }

        private void SetHiddenImmediate()
        {
            if (!_group) return;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            if (_radialRoot) _radialRoot.localScale = Vector3.one;
            isViz = false;
        }

        private bool CanAfford(BlueprintModel blueprint)
        {
            if (_inventory == null || blueprint == null) return false;
            for (int i = 0; i < blueprint.recipe.Count; i++)
                if (GetOwnedCount(blueprint.recipe[i].item) < blueprint.recipe[i].count) return false;
            return true;
        }

        private int GetOwnedCount(int itemId)
        {
            if (_inventory == null) return 0;
            int count = 0;
            var items = _inventory.GetAllItems();
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && !items[i].IsEmpty && items[i].ins.itemId == itemId)
                    count += items[i].stack;
            return count;
        }

        private void AddIngredientHint(Transform node, BlueprintModel blueprint)
        {
            var parts = new List<string>();
            for (int i = 0; i < blueprint.recipe.Count; i++)
            {
                var entry = blueprint.recipe[i];
                parts.Add($"{itemDatabase.GetItem(entry.item).itemName} {GetOwnedCount(entry.item)}/{entry.count}");
            }
            var hint = Text("Ingredients", node, string.Join("  ·  ", parts), 10, FontStyle.Bold,
                new Color(.7f, .89f, .91f), TextAnchor.MiddleCenter);
            Anchor(hint.rectTransform, new Vector2(.5f, .5f), new Vector2(220, 24), new Vector2(0, -76));
        }

        private void CreateConnector(Vector2 from, Vector2 to, float alpha, bool recipe = false)
        {
            var image = Image(recipe ? "RecipeConnector" : "CategoryConnector", _connectorRoot,
                new Color(Cyan.r, Cyan.g, Cyan.b, alpha), false);
            Vector2 delta = to - from;
            var rect = image.rectTransform;
            Anchor(rect, new Vector2(.5f, .5f), new Vector2(delta.magnitude, 2f), (from + to) * .5f);
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void ClearConnectors()
        {
            for (int i = _connectorRoot.childCount - 1; i >= 0; i--)
                Destroy(_connectorRoot.GetChild(i).gameObject);
        }

        private void ClearRecipeConnectors()
        {
            for (int i = _connectorRoot.childCount - 1; i >= 0; i--)
                if (_connectorRoot.GetChild(i).name == "RecipeConnector") Destroy(_connectorRoot.GetChild(i).gameObject);
        }

        private bool TryGetResult(BlueprintModel blueprint, out _001_Scripts.Data.Item.Item result)
        {
            result = null;
            if (blueprint == null || !itemDatabase) return false;
            try
            {
                result = itemDatabase.GetItem(blueprint.resultCraft);
                return result != null;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        private void SetStatus(string value, bool positive)
        {
            if (!_status) return;
            _status.text = value;
            _status.color = positive ? new Color(.42f, 1f, .76f) : new Color(1f, .68f, .38f);
        }

        private static void PrepareNodesForAnimation(List<RadialNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Rect.anchoredPosition = Vector2.zero;
                nodes[i].Rect.localScale = Vector3.one * .25f;
                nodes[i].Group.alpha = 0f;
            }
        }

        private void AnimateNodes(List<RadialNode> nodes, float elapsed, float duration)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                float t = Smooth01((elapsed - i * nodeDelay) / Mathf.Max(.1f, duration * .72f));
                nodes[i].Rect.anchoredPosition = Vector2.LerpUnclamped(Vector2.zero, nodes[i].Target, t);
                nodes[i].Rect.localScale = Vector3.one * Mathf.LerpUnclamped(.25f, 1f, t);
                nodes[i].Group.alpha = t;
            }
        }

        private static void FinishNodes(List<RadialNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Rect.anchoredPosition = nodes[i].Target;
                nodes[i].Rect.localScale = Vector3.one;
                nodes[i].Group.alpha = 1f;
            }
        }

        private static float GetCategoryAngle(int index, int count)
        {
            if (count <= 1) return 0f;
            return Mathf.Lerp(65f, -65f, index / (count - 1f));
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static string DisplayName(BlueprintModel blueprint, string fallback)
            => string.IsNullOrWhiteSpace(blueprint.bluePrintName) ? fallback : blueprint.bluePrintName;

        private static string CategoryLabel(ItemType category) => category switch
        {
            ItemType.materials => "재료",
            ItemType.weapon => "도구",
            ItemType.armor => "장비",
            ItemType.consumable => "소모품",
            _ => category.ToString()
        };

        private static string CategoryGlyph(ItemType category) => category switch
        {
            ItemType.materials => "⬡",
            ItemType.weapon => "⌁",
            ItemType.armor => "◈",
            ItemType.consumable => "+",
            _ => "◇"
        };

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static void ClearObjects(List<GameObject> objects)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i]) Destroy(objects[i]);
            objects.Clear();
        }

        private static Button CreateCircleButton(string name, Transform parent, float size, string glyph,
            string label, Color color)
        {
            var circle = Circle(name, parent, color, 0f);
            Anchor(circle.rectTransform, new Vector2(.5f, .5f), new Vector2(size, size), Vector2.zero);
            var button = circle.gameObject.AddComponent<Button>();
            button.targetGraphic = circle;
            circle.gameObject.AddComponent<SurvivalUIInteractableFX>();

            var ring = Circle("Ring", circle.transform, new Color(1f, 1f, 1f, .42f), .84f);
            ring.raycastTarget = false;
            Stretch(ring.rectTransform, Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5));

            var glow = Circle("Glow", circle.transform, new Color(Cyan.r, Cyan.g, Cyan.b, .13f), 0f);
            glow.raycastTarget = false;
            Stretch(glow.rectTransform, Vector2.zero, Vector2.one, new Vector2(13, 13), new Vector2(-13, -13));

            var glyphText = Text("Glyph", circle.transform, glyph, Mathf.RoundToInt(size * .35f), FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            Stretch(glyphText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6, 14), new Vector2(-6, -10));
            var labelText = Text("Label", circle.transform, label, Mathf.RoundToInt(size * .12f), FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            Anchor(labelText.rectTransform, new Vector2(.5f, 0), new Vector2(size * 1.8f, 28), new Vector2(0, -25));
            return button;
        }

        private static RadialCircleGraphic Circle(string name, Transform parent, Color color, float innerRadius)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RadialCircleGraphic));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var circle = go.GetComponent<RadialCircleGraphic>();
            circle.color = color;
            circle.InnerRadius = innerRadius;
            return circle;
        }

        private static Image Image(string name, Transform parent, Color color, bool circle)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = Resources.GetBuiltinResource<Sprite>(circle ? "UI/Skin/Knob.psd" : "UI/Skin/UISprite.psd");
            image.type = circle ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
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

        private void OnDestroy()
        {
            if (_hubButton) _hubButton.onClick.RemoveListener(RequestClose);
            _subscriptions?.Dispose();
        }
    }
}
