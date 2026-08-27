using AstraNope.Data.Messages;
using AstraNope.Data.Messages.Player;
using AstraNope.Contracts.WorldObjects;
using AstraNope.WorldObjects.Entities;
using AstraNope.Contracts;
using AstraNope.WorldObjects.Items;
using AstraNope.WorldObjects.Structures;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.Types.States;
using MessagePipe;
using UnityEngine;
using VContainer;

using AstraNope.Localization;
namespace AstraNope.WorldObjects.Vehicles.Components
{
    public enum HatchType
    {
        SmallSeat,
        LargeEntrance,
        LargeSeat,
        LargeExit
    }

    public class HatchComponent : InteractableComponentBase
    {
        [SerializeField] private HatchType type;
        [SerializeField] private SeatComponent linkedSeat;
        [SerializeField] private Transform interiorSpawnPoint;
        [SerializeField] private Transform exteriorSpawnPoint;
        [SerializeField] private LargeSubVehicle parentVehicle;

        private IPublisher<PlayerVehicleStateMessage> _statePublisher;
        private IPlayerContext _playerContext;

        [Inject]
        public void Construct(
            IPublisher<PlayerVehicleStateMessage> statePublisher,
            IPlayerContext playerContext)
        {
            _statePublisher = statePublisher;
            _playerContext = playerContext;
        }

        protected override string DefaultInteractionLabel => "Enter";

        public void ConfigureSmallSeat(SeatComponent seat)
        {
            type = HatchType.SmallSeat;
            linkedSeat = seat;
            ConfigureInteraction(L10n.T("k_84a4e71fab"), "LMB");
        }

        public override void Interact()
        {
            var player = _playerContext.PlayerTrs;
            switch (type)
            {
                case HatchType.SmallSeat:
                    if (linkedSeat) linkedSeat.Sit(player);
                    break;
                case HatchType.LargeEntrance:
                    EnterLarge(player);
                    break;
                case HatchType.LargeSeat:
                    linkedSeat.Sit(player);
                    break;
                case HatchType.LargeExit:
                    ExitToOutside(player);
                    break;
            }
        }

        private void EnterLarge(Transform player)
        {
            player.SetParent(parentVehicle.InteriorAnchor);
            player.position = interiorSpawnPoint.position;
            player.rotation = interiorSpawnPoint.rotation;
            _statePublisher.Publish(new PlayerVehicleStateMessage(PlayerVehicleState.InsideLarge));
        }

        private void ExitToOutside(Transform player)
        {
            player.SetParent(null);
            player.position = exteriorSpawnPoint.position;
            player.rotation = exteriorSpawnPoint.rotation;
            _statePublisher.Publish(new PlayerVehicleStateMessage(PlayerVehicleState.None));
        }
    }
}
