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

namespace _001_Scripts.UI
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BlueprintPanel : PanelBase
    {
        [SerializeField] private Text summary;
        [SerializeField] private ModalNavigation navigation;
        [SerializeField] private List<BlueprintTileView> tiles = new();
        [SerializeField] private BlueprintRecipeTooltip recipeTooltip;
        [SerializeField, HideInInspector] private int editorLayoutVersion;
        private IBlueprintProgressReader _progress;
        private BluePrintDataBase _database;
        private IItemCatalog _itemCatalog;
        private IBuildingPlacementService _buildingPlacement;
        private INotificationService _notifications;
        private IPublisher<UIReqMessage> _uiPublisher;
        private IDisposable _bag;

        private new void Awake()
        {
            base.Awake();
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 120;
            SurvivalUITheme.ConfigureCanvas(gameObject, 1600f, 900f);
        }

        [Inject]
        private void Construct(IBlueprintProgressReader progress, BluePrintDataBase database, IItemCatalog itemCatalog,
            IBuildingPlacementService buildingPlacement, INotificationService notifications,
            ISubscriber<BlueprintProgressChangedMessage> changed,
            IPublisher<UIReqMessage> uiPublisher)
        {
            _bag?.Dispose();
            _progress = progress;
            _database = database;
            _itemCatalog = itemCatalog;
            _buildingPlacement = buildingPlacement;
            _notifications = notifications;
            _uiPublisher = uiPublisher;
            navigation?.Bind(uiPublisher);
            var builder = DisposableBag.CreateBuilder();
            builder.Add(changed.Subscribe(_ => Refresh()));
            _bag = builder.Build();
            Refresh();
        }

        public override void Open()
        {
            base.Open();
            recipeTooltip?.Hide();
            Refresh();
        }

        public override void Close()
        {
            recipeTooltip?.Hide();
            base.Close();
        }

        private void Refresh()
        {
            if (_progress == null) return;
            int unlocked = 0;
            int total = 0;
            var statuses = _progress.GetAllBlueprints();
            var byId = new Dictionary<int, _001_Scripts.Data.BlueprintUnlockStatus>();
            for (int i = 0; i < statuses.Count; i++)
            {
                byId[statuses[i].Id] = statuses[i];
                total++;
                if (statuses[i].IsUnlocked) unlocked++;
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                if (!tile) continue;
                tile.BindHover(ShowRecipeTooltip, HideRecipeTooltip);
                tile.BindSelection(SelectBlueprint);
                if (byId.TryGetValue(tile.BlueprintId, out var status)) tile.Show(status);
                else tile.gameObject.SetActive(false);
            }
            if (summary) summary.text = $"해금 {unlocked} / {total}   •   진행 중 {total - unlocked}";
        }

        private void ShowRecipeTooltip(int blueprintId, RectTransform anchor)
        {
            if (_database == null || recipeTooltip == null) return;
            bool isUnlocked = _progress != null &&
                              _progress.TryGetBlueprint(blueprintId, out var status) &&
                              status.IsUnlocked;
            recipeTooltip.Show(_database.GetBluePrint(blueprintId), _itemCatalog, anchor, isUnlocked);
        }

        private void HideRecipeTooltip() => recipeTooltip?.Hide();

        private void SelectBlueprint(int blueprintId)
        {
            if (_progress == null || !_progress.TryGetBlueprint(blueprintId, out var status) || !status.IsUnlocked)
            {
                _notifications?.Show("청사진 잠김", "아직 청사진이 해금되지 않았습니다.", "!",
                    NotificationKind.Warning, 3f);
                return;
            }

            string failure = string.Empty;
            if (_buildingPlacement == null || !_buildingPlacement.TryBegin(blueprintId, out failure))
            {
                if (!string.IsNullOrWhiteSpace(failure))
                    _notifications?.Show("건축 배치", failure, "!", NotificationKind.Warning, 3f);
                return;
            }

            recipeTooltip?.Hide();
            _uiPublisher?.Publish(new UIReqMessage(UIReqMsgType.Close, "Blueprint"));
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
