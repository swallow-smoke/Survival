using System;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Controller
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public Transform _trs;

        [SerializeField] private float sensitivity = 100f;
        [SerializeField] private float pitchMin = -80;
        [SerializeField] private float pitchMax = 80;
        [SerializeField, Tooltip("Player root rotated by first-person yaw.")]
        private Transform bodyRoot;
        private float pitch;
        private float yaw;

        private Vector2 lookVector;

        [SerializeField] private Renderer player;

        [Header("Head Bob")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField, Min(0f)] private float walkFrequency = 8f;
        [SerializeField, Min(0f)] private float runFrequency = 12f;
        [SerializeField] private Vector2 walkAmplitude = new(.025f, .04f);
        [SerializeField] private Vector2 runAmplitude = new(.045f, .07f);
        [SerializeField, Min(.01f)] private float bobSmoothing = 14f;

        private IDisposable _bag;
        private IInputService _input;
        private bool curCamState = true;
        private bool _vehicleMode;
        private Transform _originalParent;
        private Vector3 _originalLocalPos;
        private Vector3 _viewBaseLocalPos;
        private Vector3 _bobOffset;
        private float _bobTime;
        private float _moveAmount;
        private bool _isGrounded;
        private bool _isSwimming;

        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        public Transform ViewTransform => _trs;

        private void Awake()
        {
            if (player) player.enabled = false;
            if (!_trs) _trs = transform;
            if (!bodyRoot) bodyRoot = _trs.root;

            if (_trs != bodyRoot && _trs.parent != bodyRoot)
                _trs.SetParent(bodyRoot, true);

            _viewBaseLocalPos = _trs.localPosition;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            float rawPitch = _trs.localEulerAngles.x;
            pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
            yaw = bodyRoot ? bodyRoot.eulerAngles.y : _trs.eulerAngles.y;

            if (_input == null) return;

            _input.OnLook += HandleLook;
        }

        private void Locker(PlayerUIStateMsg msg)
        {
            switch (msg.state)
            {
                case PlayerUIState.Inventory:
                case PlayerUIState.Log:
                case PlayerUIState.Blueprint:
                case PlayerUIState.Workbench:
                case PlayerUIState.SubmarineFabricator:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    curCamState = false;
                    break;
                default:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    curCamState = true;
                    break;
            }

            ApplyStoredViewRotation();
        }

        private void HandleLook(Vector2 value)
        {
            if (!curCamState || _vehicleMode) return;

            lookVector = value;

            pitch -= lookVector.y * sensitivity;
            yaw += lookVector.x * sensitivity;
        }

        private void OnVehicleControlAssigned(VehicleControlAssignedMsg msg)
        {
            if (msg.Controller != null)
            {
                _originalParent = _trs.parent;
                _originalLocalPos = _viewBaseLocalPos;
                _trs.SetParent(msg.Controller.CameraAnchor);
                _trs.localPosition = Vector3.zero;
                _trs.localRotation = Quaternion.identity;
                _viewBaseLocalPos = Vector3.zero;
                _bobOffset = Vector3.zero;
                _vehicleMode = true;
            }
            else
            {
                _trs.SetParent(_originalParent);
                _trs.localPosition = _originalLocalPos;
                _viewBaseLocalPos = _originalLocalPos;
                float rawPitch = _trs.localEulerAngles.x;
                pitch = rawPitch > 180f ? rawPitch - 360f : rawPitch;
                yaw = bodyRoot ? bodyRoot.eulerAngles.y : _trs.eulerAngles.y;
                _vehicleMode = false;
            }
        }

        private void OnMovement(PlayerMovementMessage message)
        {
            _moveAmount = Mathf.Clamp01(message.velocity);
            _isGrounded = message.isGround;
            _isSwimming = message.isSwimming;
        }

        private void LateUpdate()
        {
            if (player && player.enabled) player.enabled = false;

            ApplyStoredViewRotation();

            UpdateHeadBob(curCamState && !_vehicleMode);
        }

        private void ApplyStoredViewRotation()
        {
            if (_vehicleMode) return;

            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            if (bodyRoot) bodyRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            _trs.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateHeadBob(bool cameraActive)
        {
            bool moving = enableHeadBob && cameraActive && _isGrounded && !_isSwimming && _moveAmount > .025f;
            Vector3 targetOffset = Vector3.zero;

            if (moving)
            {
                float speed01 = Mathf.InverseLerp(.15f, 1f, _moveAmount);
                float frequency = Mathf.Lerp(walkFrequency, runFrequency, speed01);
                Vector2 amplitude = Vector2.Lerp(walkAmplitude, runAmplitude, speed01) * _moveAmount;
                _bobTime += Time.deltaTime * frequency;
                targetOffset.x = Mathf.Cos(_bobTime * .5f) * amplitude.x;
                targetOffset.y = Mathf.Sin(_bobTime) * amplitude.y;
            }
            else
            {
                _bobTime = 0f;
            }

            float blend = 1f - Mathf.Exp(-bobSmoothing * Time.deltaTime);
            _bobOffset = Vector3.Lerp(_bobOffset, targetOffset, blend);
            _trs.localPosition = _viewBaseLocalPos + _bobOffset;
        }

        [Inject]
        private void Constructor(
            ISubscriber<PlayerUIStateMsg> iPlayerUIStateSub,
            ISubscriber<VehicleControlAssignedMsg> vehicleControlSub,
            ISubscriber<PlayerMovementMessage> movementSubscriber,
            IInputService inputService)
        {
            _input = inputService;

            var builder = DisposableBag.CreateBuilder();

            iPlayerUIStateSub.Subscribe(Locker).AddTo(builder);
            vehicleControlSub.Subscribe(OnVehicleControlAssigned).AddTo(builder);
            movementSubscriber.Subscribe(OnMovement).AddTo(builder);

            _bag = builder.Build();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnLook -= HandleLook;
            }

            _bag?.Dispose();
        }
    }
}
