using System;
using System.Collections.Generic;
using AstraNope.UI.Base;
using AstraNope.Data;
using AstraNope.Data.Messages;
using AstraNope.Contracts;
using AstraNope.UI.Components;
using MessagePipe;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

using AstraNope.Localization;
namespace AstraNope.UI.Panels
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LogPanel : PanelBase
    {
        [SerializeField] private ModalNavigation navigation;
        [SerializeField] private List<LogEntryView> entryViews = new();
        [SerializeField] private Text detailTitle;
        [SerializeField] private Text detailBody;
        [SerializeField] private Text emptyLabel;
        [SerializeField] private Image detailImage;
        private IDisposable _bag;
        private ILogCollectionReader _logs;
        private IReadOnlyList<LogEntry> _entries = Array.Empty<LogEntry>();
        private int _selectedIndex = -1;

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
        private void Construct(ILogCollectionReader logs,
            ISubscriber<LogCollectionChangedMessage> changed,
            IPublisher<UIReqMessage> uiPublisher)
        {
            _bag?.Dispose();
            _logs = logs;
            navigation?.Bind(uiPublisher);
            var builder = DisposableBag.CreateBuilder();
            builder.Add(changed.Subscribe(message => Refresh(message.LogId)));
            _bag = builder.Build();
            Refresh();
        }

        public override void Open()
        {
            base.Open();
            Refresh();
        }

        private void Refresh(string preferredId = null)
        {
            if (_logs == null) return;
            _entries = _logs.GetAllLogs();
            _selectedIndex = FindSelected(preferredId);
            for (int i = 0; i < entryViews.Count; i++)
            {
                if (i < _entries.Count)
                {
                    entryViews[i].Show(i, _entries[i].title, Select);
                    entryViews[i].SetSelected(i == _selectedIndex);
                }
                else entryViews[i].gameObject.SetActive(false);
            }
            if (emptyLabel) emptyLabel.gameObject.SetActive(_entries.Count == 0);
            ShowDetails(_selectedIndex >= 0 ? _entries[_selectedIndex] : null);
        }

        private int FindSelected(string preferredId)
        {
            if (!string.IsNullOrWhiteSpace(preferredId))
                for (int i = 0; i < _entries.Count; i++)
                    if (string.Equals(_entries[i].id, preferredId, StringComparison.OrdinalIgnoreCase)) return i;
            return _entries.Count > 0 ? 0 : -1;
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _entries.Count) return;
            _selectedIndex = index;
            for (int i = 0; i < entryViews.Count; i++) entryViews[i].SetSelected(i == index);
            ShowDetails(_entries[index]);
        }

        private void ShowDetails(LogEntry entry)
        {
            if (detailTitle) detailTitle.text = entry?.title ?? L10n.T("k_aaf8e68048");
            if (detailBody) detailBody.text = entry?.body ?? string.Empty;
            if (!detailImage) return;
            detailImage.sprite = entry?.image;
            detailImage.color = entry?.image ? Color.white : new Color(.13f, .08f, .24f, .66f);
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
