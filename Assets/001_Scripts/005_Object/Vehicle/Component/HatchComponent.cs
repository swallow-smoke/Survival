using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
using _001_Scripts.Structure;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Vehicle.Component
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

        private IPublisher<PlayerVehicleStateMsg> _statePublisher;
        private IPlayerContext _playerContext;

        [Inject]
        public void Construct(
            IPublisher<PlayerVehicleStateMsg> statePublisher,
            IPlayerContext playerContext)
        {
            _statePublisher = statePublisher;
            _playerContext = playerContext;
        }

        protected override string DefaultInteractionLabel => "Enter";

        public override void Interact()
        {
            var player = _playerContext.PlayerTrs;
            switch (type)
            {
                case HatchType.SmallSeat:
                    linkedSeat.Sit(player);
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
            _statePublisher.Publish(new PlayerVehicleStateMsg(PlayerVehicleState.InsideLarge));
        }

        private void ExitToOutside(Transform player)
        {
            player.SetParent(null);
            player.position = exteriorSpawnPoint.position;
            player.rotation = exteriorSpawnPoint.rotation;
            _statePublisher.Publish(new PlayerVehicleStateMsg(PlayerVehicleState.None));
        }
    }
}
