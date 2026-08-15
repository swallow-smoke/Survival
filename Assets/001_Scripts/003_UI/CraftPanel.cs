using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using _001_Scripts.UI.Component;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using BluePrintModel = _001_Scripts.Data.BluePrint.BluePrint;

namespace _001_Scripts.UI
{
    public class CraftPanel : PanelBase
    {
        [Header("Data")]
        [SerializeField] private BluePrintDataBase bpDB;
        [SerializeField] private ItemDataBase itemDB;
        [SerializeField, HideInInspector] private int editorStyleVersion;
        [SerializeField, HideInInspector] private int editorLayoutVersion;

        [Header("UGUI - Recipe List")]
        [SerializeField] private GameObject blueprintSlotPrefab;
        [SerializeField] private Transform listParent;

        [Header("UGUI - Details")]
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailMeta;
        [SerializeField] private Text detailDescription;
        [SerializeField] private Text previewGlyph;
        [SerializeField] private Transform ingredientParent;
        [SerializeField] private GameObject ingredientTemplate;

        [Header("UGUI - Actions")]
        [SerializeField] private Text resultText;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button inventoryTabButton;
        [SerializeField] private Button craftTabButton;

        private readonly List<BlueprintSlot> _slots = new();
        private readonly List<GameObject> _ingredientRows = new();
        private IDisposable _bag;
        private IInventoryService _inventory;
        private IPublisher<CraftReqMessage> _craftReqPublisher;
        private IPublisher<UIReqMessage> _uiReqPublisher;
        private BluePrintModel _selected;
        private float _lastCraftRequestTime = -10f;

        private void Start()
        {
            if (closeButton)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
                closeButton.onClick.AddListener(OnCloseClicked);
            }
            if (craftButton)
            {
                craftButton.onClick.RemoveListener(CraftSelected);
                craftButton.onClick.AddListener(CraftSelected);
            }
            if (inventoryTabButton)
            {
                inventoryTabButton.onClick.RemoveListener(OnInventoryTabClicked);
                inventoryTabButton.onClick.AddListener(OnInventoryTabClicked);
            }
            if (craftTabButton)
            {
                craftTabButton.onClick.RemoveListener(OnCraftTabClicked);
                craftTabButton.onClick.AddListener(OnCraftTabClicked);
            }
        }

        private void BuildSlots()
        {
            if (!blueprintSlotPrefab || !listParent)
            {
                Debug.LogError("[CraftPanel] UGUI recipe prefab or content is not assigned.", this);
                return;
            }

            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);
            _slots.Clear();

            var blueprints = bpDB.GetAllBluePrints();
            for (int i = 0; i < blueprints.Count; i++)
            {
                var blueprint = blueprints[i];
                if (!blueprint.isUnlocked) continue;
                var go = Instantiate(blueprintSlotPrefab, listParent);
                go.SetActive(true);
                var slot = go.GetComponent<BlueprintSlot>();
                slot.Init(blueprint, itemDB, SelectBlueprint);
                _slots.Add(slot);
            }

            if (_slots.Count > 0)
                SelectBlueprint(_slots[0].Blueprint);
            else
            {
                SetText(detailName, "사용 가능한 설계도가 없습니다");
                SetText(detailMeta, "NO BLUEPRINTS");
                SetText(detailDescription, "탐사하며 새로운 설계도를 발견하세요.");
                if (craftButton) craftButton.interactable = false;
            }
        }

        private void SelectBlueprint(BluePrintModel blueprint)
        {
            _selected = blueprint;
            var result = itemDB.GetItem(blueprint.resultCraft);
            SetText(detailName, string.IsNullOrWhiteSpace(blueprint.bluePrintName)
                ? result.itemName
                : blueprint.bluePrintName);
            SetText(detailMeta, $"LEVEL {blueprint.requiredLevel}   •   제작 시간 {blueprint.craftTime:0.#}초");
            SetText(detailDescription, string.IsNullOrWhiteSpace(result.itemDesc)
                ? "생존과 탐사에 필요한 아이템을 제작합니다."
                : result.itemDesc);
            SetText(previewGlyph, InventoryPanel.GetGlyph(result.itemType));
            SetText(resultText, "재료를 확인한 뒤 제작 버튼을 누르세요.");
            if (resultText) resultText.color = SurvivalUITheme.TextMuted;

            for (int i = 0; i < _slots.Count; i++)
                _slots[i].SetSelected(_slots[i].Blueprint == blueprint);
            RefreshIngredients();
        }

        private void RefreshIngredients()
        {
            for (int i = 0; i < _ingredientRows.Count; i++)
                if (_ingredientRows[i]) Destroy(_ingredientRows[i]);
            _ingredientRows.Clear();

            if (_selected == null || !ingredientParent || !ingredientTemplate || _inventory == null)
            {
                if (craftButton) craftButton.interactable = false;
                return;
            }

            bool affordable = true;
            for (int i = 0; i < _selected.recipe.Count; i++)
            {
                var entry = _selected.recipe[i];
                int owned = GetOwnedCount(entry.item);
                bool enough = owned >= entry.count;
                affordable &= enough;

                var row = Instantiate(ingredientTemplate, ingredientParent);
                row.name = $"Ingredient_{i:00}";
                row.SetActive(true);
                SetText(row.transform.Find("Icon")?.GetComponent<Text>(),
                    InventoryPanel.GetGlyph(itemDB.GetItem(entry.item).itemType));
                SetText(row.transform.Find("Name")?.GetComponent<Text>(), itemDB.GetItem(entry.item).itemName);
                var count = row.transform.Find("Count")?.GetComponent<Text>();
                SetText(count, $"{owned} / {entry.count}");
                if (count) count.color = enough ? new Color(0.84f, 0.76f, 1f) : SurvivalUITheme.Danger;
                _ingredientRows.Add(row);
            }

            if (craftButton) craftButton.interactable = affordable;
        }

        private int GetOwnedCount(int itemId)
        {
            int count = 0;
            var items = _inventory.GetAllItems();
            for (int i = 0; i < items.Count; i++)
                if (items[i] != null && !items[i].IsEmpty && items[i].ins.itemId == itemId)
                    count += items[i].stack;
            return count;
        }

        private void CraftSelected()
        {
            if (_selected == null || !craftButton || !craftButton.interactable) return;
            if (Time.unscaledTime - _lastCraftRequestTime < .25f) return;
            _lastCraftRequestTime = Time.unscaledTime;
            _craftReqPublisher.Publish(new CraftReqMessage(_selected.bluePrintName));
        }

        private void OnCraftResult(CraftResultMessage message)
        {
            bool success = message.msgType == CraftMessageType.Success;
            SetText(resultText, success
                ? $"제작 완료: {itemDB.GetItem(message.itemId).itemName}"
                : "제작 실패: 필요한 재료가 부족합니다.");
            if (resultText)
                resultText.color = success ? new Color(0.84f, 0.76f, 1f) : SurvivalUITheme.Danger;
            RefreshIngredients();
        }

        private void OnInventoryChanged(InvChangedMessage message) => RefreshIngredients();

        public override void Open()
        {
            base.Open();
            RefreshIngredients();
        }

        public void OnCloseClicked()
        {
            if (_uiReqPublisher != null)
                _uiReqPublisher.Publish(new UIReqMessage(UIReqMsgType.Close, "Craft"));
            else
                Close();
        }

        public void OnInventoryTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Inventory"));
        }

        public void OnCraftTabClicked()
        {
            _uiReqPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Craft"));
            RefreshIngredients();
        }

        [Inject]
        private void Construct(IPublisher<CraftReqMessage> craftReqPublisher,
            IPublisher<UIReqMessage> uiReqPublisher,
            ISubscriber<CraftResultMessage> craftResultSubscriber,
            ISubscriber<InvChangedMessage> invChangedSubscriber,
            IInventoryService inventory)
        {
            _bag?.Dispose();
            _craftReqPublisher = craftReqPublisher;
            _uiReqPublisher = uiReqPublisher;
            _inventory = inventory;

            var builder = DisposableBag.CreateBuilder();
            builder.Add(craftResultSubscriber.Subscribe(OnCraftResult));
            builder.Add(invChangedSubscriber.Subscribe(OnInventoryChanged));
            _bag = builder.Build();
            BuildSlots();
        }

        private static void SetText(Text target, string value)
        {
            if (target) target.text = value;
        }

        private void OnDestroy()
        {
            _bag?.Dispose();
            if (closeButton) closeButton.onClick.RemoveListener(OnCloseClicked);
            if (craftButton) craftButton.onClick.RemoveListener(CraftSelected);
            if (inventoryTabButton) inventoryTabButton.onClick.RemoveListener(OnInventoryTabClicked);
            if (craftTabButton) craftTabButton.onClick.RemoveListener(OnCraftTabClicked);
        }
    }
}
