using System;
using System.Collections.Generic;
using _001_Scripts.Base;
using _001_Scripts.Data.Message;
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
        [SerializeField, HideInInspector] private int editorLayoutVersion;
        private IBlueprintProgressReader _progress;
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
        private void Construct(IBlueprintProgressReader progress,
            ISubscriber<BlueprintProgressChangedMessage> changed,
            IPublisher<UIReqMessage> uiPublisher)
        {
            _bag?.Dispose();
            _progress = progress;
            navigation?.Bind(uiPublisher);
            var builder = DisposableBag.CreateBuilder();
            builder.Add(changed.Subscribe(_ => Refresh()));
            _bag = builder.Build();
            Refresh();
        }

        public override void Open()
        {
            base.Open();
            Refresh();
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
                if (byId.TryGetValue(tile.BlueprintId, out var status)) tile.Show(status);
                else tile.gameObject.SetActive(false);
            }
            if (summary) summary.text = $"해금 {unlocked} / {total}   •   진행 중 {total - unlocked}";
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
