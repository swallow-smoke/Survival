using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    /// <summary>
    /// 건축 도구를 들고 우클릭했을 때 건축 진입점을 연다.
    /// 직전에 선택한 청사진이 있으면 즉시 배치를 시작하고, 없으면 청사진 패널을 띄운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildToolController : MonoBehaviour
    {
        private IHeldItemInput _input;
        private IBuildingPlacementService _placement;
        private IBuildSelectionReader _selection;
        private INotificationService _notifications;
        private IPublisher<UIReqMessage> _uiPublisher;
        private FirstPersonItemHolder _itemHolder;
        private PlayerUIState _uiState;
        private IDisposable _bag;

        [Inject]
        private void Construct(IHeldItemInput input, IBuildingPlacementService placement,
            IBuildSelectionReader selection, INotificationService notifications,
            IPublisher<UIReqMessage> uiPublisher, ISubscriber<PlayerUIStateMsg> uiStateSubscriber)
        {
            _bag?.Dispose();
            _input = input;
            _placement = placement;
            _selection = selection;
            _notifications = notifications;
            _uiPublisher = uiPublisher;
            var builder = DisposableBag.CreateBuilder();
            builder.Add(uiStateSubscriber.Subscribe(msg => _uiState = msg.state));
            _bag = builder.Build();
        }

        private void Start()
        {
            _itemHolder = GetComponent<FirstPersonItemHolder>();
            if (_input != null) _input.OnSecondaryAction += HandleSecondaryAction;
        }

        private void HandleSecondaryAction()
        {
            if (_placement == null || _placement.IsPlacing) return;
            if (_uiState != PlayerUIState.None) return;
            if (!IsHoldingBuildTool()) return;

            int blueprintId = _selection?.LastBlueprintId ?? -1;
            if (blueprintId >= 0)
            {
                if (_placement.TryBegin(blueprintId, out string failure)) return;
                if (!string.IsNullOrWhiteSpace(failure))
                    _notifications?.Show("건축 배치", failure, "!", NotificationKind.Warning, 3f);
            }

            _uiPublisher?.Publish(new UIReqMessage(UIReqMsgType.Open, "Blueprint"));
        }

        private bool IsHoldingBuildTool()
            => _itemHolder && _itemHolder.HeldItem != null && _itemHolder.HeldItem.HasFeature<IBuildTool>();

        private void OnDestroy()
        {
            if (_input != null) _input.OnSecondaryAction -= HandleSecondaryAction;
            _bag?.Dispose();
        }
    }
}
