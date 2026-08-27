using System;
using System.Collections.Generic;
using System.Text;
using AstraNope.UI.Base;
using AstraNope.Data.Messages;
using AstraNope.Data.Databases;
using AstraNope.Contracts;
using AstraNope.UI.Components;
using MessagePipe;
using UnityEngine;
using VContainer;
using BlueprintModel = AstraNope.Data.Blueprints.BluePrint;

using AstraNope.Localization;
namespace AstraNope.UI.Panels
{
    public sealed class WorkbenchPanel : PanelBase
    {
        public const int CurrentVisualVersion = 14;
        private const string LegacyRootName = "WorkbenchRadialRoot";

        [Header("Data")]
        [SerializeField] private BluePrintDataBase blueprintDatabase;
        [SerializeField] private ItemDataBase itemDatabase;
        [SerializeField, HideInInspector] private int visualVersion;

        private CanvasGroup _group;
        private SimpleRadialMenuView _radial;
        private IInventoryReader _inventory;
        private IHotbarReader _hotbar;
        private IPublisher<CraftReqMessage> _craftPublisher;
        private IPublisher<UIReqMessage> _uiPublisher;
        private IDisposable _subscriptions;
        private string _currentPath = string.Empty;
        private float _lastCraftTime = -10f;
        private bool _refreshPending;
        private bool _building;
        private int _pinnedBlueprintId = -1;

        public int VisualVersion => visualVersion;

        private new void Awake()
        {
            base.Awake();
            EnsureView();
            SetHidden();
        }

        [Inject]
        private void Construct(IPublisher<CraftReqMessage> craftPublisher,
            IPublisher<UIReqMessage> uiPublisher,
            ISubscriber<CraftResultMessage> craftResults,
            ISubscriber<InventoryChangedMessage> inventoryChanges,
            IInventoryReader inventory,
            IHotbarReader hotbar)
        {
            _subscriptions?.Dispose();
            _craftPublisher = craftPublisher;
            _uiPublisher = uiPublisher;
            _inventory = inventory;
            _hotbar = hotbar;
            var builder = DisposableBag.CreateBuilder();
            builder.Add(craftResults.Subscribe(OnCraftResult));
            builder.Add(inventoryChanges.Subscribe(_ => Refresh()));
            _subscriptions = builder.Build();
        }

        public override void Open()
        {
            EnsureData();
            EnsureView();
            _currentPath = string.Empty;
            BuildCurrentLevel();
            UpdatePinnedRecipe();
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
            isViz = true;
            _radial.PlayOpenAnimation();
        }

        public override void Close()
        {
            isViz = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _radial.HideTooltip();
            _radial.PlayCloseAnimation(SetHidden);
        }

        public void RebuildVisualTreeForEditor()
        {
            EnsureView();
            _radial.Rebuild("⚙");
            visualVersion = CurrentVisualVersion;
            SetHidden();
        }

        private void BuildCurrentLevel()
        {
            if (_building) return;
            _building = true;
            try
            {
            var entries = new List<SimpleRadialEntry>();
            string[] current = SplitPath(_currentPath);

            if (current.Length > 0)
                entries.Add(new SimpleRadialEntry("Back", "←", L10n.T("k_2df0686920"), GoBack, true,
                    L10n.T("k_b06c2e0212")));

            if (blueprintDatabase && itemDatabase)
            {
                var childCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var blueprints = blueprintDatabase.GetAllBluePrints();

                for (int i = 0; i < blueprints.Count; i++)
                {
                    var blueprint = blueprints[i];
                    if (blueprint == null || !blueprint.isUnlocked || !TryGetResult(blueprint, out _)) continue;
                    string[] path = SplitPath(blueprint.categoryPath);
                    if (!StartsWith(path, current)) continue;

                    if (path.Length > current.Length)
                    {
                        string child = path[current.Length];
                        if (!childCategories.Add(child)) continue;
                        string targetPath = CombinePath(_currentPath, child);
                        entries.Add(new SimpleRadialEntry($"Category_{child}", "○", child,
                            () => EnterCategory(targetPath), true, L10n.F("k_c6d14f0613", child)));
                        continue;
                    }

                    AddRecipe(entries, blueprint);
                }
            }

            _radial.SetEntries(entries);
            }
            finally
            {
                _building = false;
            }
        }

        private void AddRecipe(List<SimpleRadialEntry> entries, BlueprintModel blueprint)
        {
            if (!TryGetResult(blueprint, out var result)) return;
            bool affordable = CanAfford(blueprint);
            string label = string.IsNullOrWhiteSpace(blueprint.bluePrintName)
                ? result.itemName
                : blueprint.bluePrintName;
            string tooltip = BuildRecipeText(blueprint, label, true);
            entries.Add(new SimpleRadialEntry($"Recipe_{blueprint.bluePrintId}",
                InventoryPanel.GetGlyph(result.itemType), label, () => Craft(blueprint), affordable, tooltip,
                () => PinRecipe(blueprint), BuildRecipeTooltip(blueprint, label)));
        }

        private SimpleRadialRecipeTooltipData BuildRecipeTooltip(BlueprintModel blueprint, string label)
        {
            var ingredients = new List<SimpleRadialIngredientData>();
            if (blueprint.recipe != null)
            {
                for (int i = 0; i < blueprint.recipe.Count; i++)
                {
                    var required = blueprint.recipe[i];
                    var item = TryGetItem(required.item);
                    ingredients.Add(new SimpleRadialIngredientData(item?.icon,
                        item != null ? InventoryPanel.GetGlyph(item.itemType) : "?",
                        item != null && !string.IsNullOrWhiteSpace(item.itemName)
                            ? item.itemName
                            : $"Item {required.item}", required.count));
                }
            }
            string craftTimeLabel = blueprint.craftTime.ToString("0.#");
            string description = L10n.F("k_a9fdaf210e", label, craftTimeLabel);
            return new SimpleRadialRecipeTooltipData(description, ingredients);
        }

        private void EnterCategory(string path)
        {
            _currentPath = path;
            BuildCurrentLevel();
            _radial.PlayNodeAnimation();
        }

        private void GoBack()
        {
            int separator = _currentPath.LastIndexOf('/');
            _currentPath = separator < 0 ? string.Empty : _currentPath[..separator];
            BuildCurrentLevel();
            _radial.PlayNodeAnimation();
        }

        private void Craft(BlueprintModel blueprint)
        {
            if (blueprint == null || !CanAfford(blueprint) || _craftPublisher == null) return;
            if (Time.unscaledTime - _lastCraftTime < .25f) return;
            _lastCraftTime = Time.unscaledTime;
            _craftPublisher.Publish(new CraftReqMessage(blueprint.bluePrintName));
        }

        private void OnCraftResult(CraftResultMessage result)
        {
            if (result.msgType == CraftMessageType.Success) Debug.Log("[Workbench] Craft complete.", this);
            else Debug.LogWarning("[Workbench] Missing materials.", this);
            Refresh();
        }

        private void Refresh()
        {
            if (isViz) _refreshPending = true;
        }

        private void LateUpdate()
        {
            if (!_refreshPending || !isViz) return;
            _refreshPending = false;
            BuildCurrentLevel();
            UpdatePinnedRecipe();
        }

        private bool CanAfford(BlueprintModel blueprint)
        {
            if (_inventory == null || blueprint?.recipe == null) return false;
            for (int i = 0; i < blueprint.recipe.Count; i++)
            {
                var required = blueprint.recipe[i];
                if (!_inventory.HasItem(required.item, required.count)) return false;
            }
            return true;
        }

        private void PinRecipe(BlueprintModel blueprint)
        {
            if (blueprint == null) return;
            _pinnedBlueprintId = blueprint.bluePrintId;
            UpdatePinnedRecipe();
        }

        private void UpdatePinnedRecipe()
        {
            if (_pinnedBlueprintId < 0 || !blueprintDatabase || !_radial) return;
            BlueprintModel blueprint = blueprintDatabase.GetBluePrint(_pinnedBlueprintId);
            if (blueprint == null || !TryGetResult(blueprint, out var result))
            {
                _pinnedBlueprintId = -1;
                _radial.ClearPinnedRecipe();
                return;
            }

            string label = string.IsNullOrWhiteSpace(blueprint.bluePrintName)
                ? result.itemName
                : blueprint.bluePrintName;
            _radial.SetPinnedRecipe($"📌 {label}", BuildRecipeText(blueprint, label, false));
        }

        private string BuildRecipeText(BlueprintModel blueprint, string label, bool includeControls)
        {
            var text = new StringBuilder(192);
            if (includeControls)
            {
                text.Append(label).Append('\n');
                text.Append(L10n.T("k_b215625c6b")).Append(blueprint.craftTime.ToString("0.#")).Append(L10n.T("k_62f99c3a3c"));
            }
            text.Append(L10n.T("k_8e5c12aaa1"));
            if (blueprint.recipe == null || blueprint.recipe.Count == 0)
            {
                text.Append(L10n.T("k_138cf307fd"));
            }
            else
            {
                for (int i = 0; i < blueprint.recipe.Count; i++)
                {
                    var required = blueprint.recipe[i];
                    int owned = GetOwnedCount(required.item);
                    int missing = Mathf.Max(0, required.count - owned);
                    text.Append("  ").Append(TryGetItemName(required.item)).Append("  ")
                        .Append(owned).Append(" / ").Append(required.count);
                    if (missing > 0)
                        text.Append(L10n.T("k_2138bca7d1")).Append(missing).Append("</color>");
                    else
                        text.Append(L10n.T("k_abd04f247d"));
                    text.Append('\n');
                }
            }
            if (includeControls) text.Append(L10n.T("k_2a7e26ec1b"));
            return text.ToString();
        }

        private int GetOwnedCount(int itemId)
        {
            int count = 0;
            if (_inventory != null)
            {
                var items = _inventory.GetAllItems();
                for (int i = 0; i < items.Count; i++)
                    if (items[i] != null && !items[i].IsEmpty && items[i].ins.itemId == itemId)
                        count += items[i].stack;
            }
            if (_hotbar != null)
            {
                for (int i = 0; i < _hotbar.HotbarSlotCount; i++)
                {
                    var slot = _hotbar.GetHotbarSlot(i);
                    if (slot != null && !slot.IsEmpty && slot.ins.itemId == itemId) count += slot.stack;
                }
            }
            return count;
        }

        private string TryGetItemName(int itemId)
        {
            if (!itemDatabase) return L10n.F("k_265bb9e1eb", itemId);
            try
            {
                var item = itemDatabase.GetItem(itemId);
                return string.IsNullOrWhiteSpace(item.itemName) ? L10n.F("k_265bb9e1eb", itemId) : item.itemName;
            }
            catch (KeyNotFoundException)
            {
                return L10n.F("k_265bb9e1eb", itemId);
            }
        }

        private AstraNope.Data.Items.Item TryGetItem(int itemId)
        {
            if (!itemDatabase) return null;
            try
            {
                return itemDatabase.GetItem(itemId);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private bool TryGetResult(BlueprintModel blueprint, out AstraNope.Data.Items.Item result)
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

        private void EnsureData()
        {
            if (!blueprintDatabase) blueprintDatabase = Resources.Load<BluePrintDataBase>("Data/BluePrints");
            if (!itemDatabase) itemDatabase = Resources.Load<ItemDataBase>("Data/ItemDataBase");
        }

        private void EnsureView()
        {
            _group = GetComponent<CanvasGroup>();
            if (!_group) _group = gameObject.AddComponent<CanvasGroup>();

            var legacy = transform.Find(LegacyRootName);
            if (legacy)
            {
                legacy.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(legacy.gameObject);
                else DestroyImmediate(legacy.gameObject);
            }

            _radial = GetComponent<SimpleRadialMenuView>();
            if (!_radial) _radial = gameObject.AddComponent<SimpleRadialMenuView>();
            _radial.Ensure("⚙");
            _radial.SetOutsideClick(RequestClose);
            _radial.SetPinnedCleared(OnPinnedCleared);
            visualVersion = CurrentVisualVersion;
        }

        private void OnPinnedCleared() => _pinnedBlueprintId = -1;

        private void RequestClose()
        {
            if (_uiPublisher != null)
                _uiPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Workbench"));
            else Close();
        }

        private void SetHidden()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            if (_group)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
            isViz = false;
        }

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Array.Empty<string>();
            string[] raw = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                string value = raw[i].Trim();
                if (value.Length > 0) result.Add(value);
            }
            return result.ToArray();
        }

        private static bool StartsWith(IReadOnlyList<string> path, IReadOnlyList<string> prefix)
        {
            if (path.Count < prefix.Count) return false;
            for (int i = 0; i < prefix.Count; i++)
                if (!string.Equals(path[i], prefix[i], StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static string CombinePath(string parent, string child)
            => string.IsNullOrWhiteSpace(parent) ? child : $"{parent}/{child}";

        private void OnDestroy() => _subscriptions?.Dispose();
    }
}
