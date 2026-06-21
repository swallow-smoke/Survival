using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Interface;
using _001_Scripts.Object.Vehicle;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Vehicle.Component
{
    public class SeatComponent : MonoBehaviour, ISeat
    {
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform standSpawnPoint;
        [SerializeField] private MonoBehaviour controllerBehaviour;
        [SerializeField] private PlayerVehicleState standState = PlayerVehicleState.None;
        [SerializeField] private Transform standReparentTarget;

        private IVehicleControllable _controller;
        private IPublisher<PlayerVehicleStateMsg> _statePublisher;
        private IPublisher<VehicleControlAssignedMsg> _vehicleControlPublisher;
        private Transform _playerTrs;

        public bool IsOccupied { get; private set; }
        public Transform CameraAnchor => seatAnchor;
        public IVehicleControllable Controller => _controller;

        [Inject]
        public void Construct(
            IPublisher<PlayerVehicleStateMsg> statePublisher,
            IPublisher<VehicleControlAssignedMsg> vehicleControlPublisher,
            IPlayerContext playerContext)
        {
            _statePublisher = statePublisher;
            _vehicleControlPublisher = vehicleControlPublisher;
            _playerTrs = playerContext.PlayerTrs;
        }

        private void Awake()
        {
            _controller = controllerBehaviour as IVehicleControllable;
            if (_controller == null)
                Debug.LogError($"[SeatComponent] {controllerBehaviour} does not implement IVehicleControllable");
        }

        public void Sit(Transform player)
        {
            if (IsOccupied) return;

            if (player.TryGetComponent<Rigidbody>(out var playerRb))
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            IsOccupied = true;
            player.SetParent(seatAnchor);
            player.localPosition = Vector3.zero;
            player.localRotation = Quaternion.identity;

            _controller.EnterControl();
            _statePublisher.Publish(new PlayerVehicleStateMsg(PlayerVehicleState.Seated));
            _vehicleControlPublisher.Publish(new VehicleControlAssignedMsg(_controller, this));
        }

        public void Stand(Transform player, Transform spawnPoint, Transform reparentTo)
        {
            if (!IsOccupied) return;

            IsOccupied = false;
            _controller.ExitControl();

            player.SetParent(reparentTo);
            player.position = spawnPoint != null ? spawnPoint.position : player.position;
            player.rotation = spawnPoint != null ? spawnPoint.rotation : player.rotation;

            _statePublisher.Publish(new PlayerVehicleStateMsg(standState));
            _vehicleControlPublisher.Publish(new VehicleControlAssignedMsg(null, null));
        }

        public void StandWithDefaults()
            => Stand(_playerTrs, standSpawnPoint, standReparentTarget);
    }
}
