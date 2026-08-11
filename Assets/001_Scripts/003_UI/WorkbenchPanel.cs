using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using VContainer;
using BlueprintModel = _001_Scripts.Data.BluePrint.BluePrint;

namespace _001_Scripts.UI
{
    public sealed class WorkbenchPanel : PanelBase
    {
        public const int CurrentVisualVersion = 7;
        private const string LegacyRootName = "WorkbenchRadialRoot";

        [Header("Data")]
        [SerializeField] private BluePrintDataBase blueprintDatabase;
        [SerializeField] private ItemDataBase itemDatabase;
        [SerializeField, HideInInspector] private int visualVersion;

        private CanvasGroup _group;
        private SimpleRadialMenuView _radial;
        private IInventoryReader _inventory;
        private IPublisher<CraftReqMessage> _craftPublisher;
        private IPublisher<UIReqMessage> _uiPublisher;
        private IDisposable _subscriptions;
        private string _currentPath = string.Empty;
        private float _lastCraftTime = -10f;

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
            ISubscriber<InvChangedMessage> inventoryChanges,
            IInventoryReader inventory)
        {
            _subscriptions?.Dispose();
            _craftPublisher = craftPublisher;
            _uiPublisher = uiPublisher;
            _inventory = inventory;
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
            var entries = new List<SimpleRadialEntry>();
            string[] current = SplitPath(_currentPath);

            if (current.Length > 0)
                entries.Add(new SimpleRadialEntry("Back", "←", "Back", GoBack));

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
                            () => EnterCategory(targetPath)));
                        continue;
                    }

                    AddRecipe(entries, blueprint);
                }
            }

            _radial.SetEntries(entries);
        }

        private void AddRecipe(List<SimpleRadialEntry> entries, BlueprintModel blueprint)
        {
            if (!TryGetResult(blueprint, out var result)) return;
            bool affordable = CanAfford(blueprint);
            string label = string.IsNullOrWhiteSpace(blueprint.bluePrintName)
                ? result.itemName
                : blueprint.bluePrintName;
            entries.Add(new SimpleRadialEntry($"Recipe_{blueprint.bluePrintId}",
                InventoryPanel.GetGlyph(result.itemType), label, () => Craft(blueprint), affordable));
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
            if (isViz) BuildCurrentLevel();
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
            visualVersion = CurrentVisualVersion;
        }

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
