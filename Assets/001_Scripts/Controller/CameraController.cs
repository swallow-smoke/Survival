using System;
using System.Threading;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Type.States;
using MessagePipe;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _001_Scripts.Controller
{
    // why cinemachine built-in cam controller is not working for 3.6.1;;
    // so i made this.
    // damper is not valid for this one, so I'll make it later;
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public Transform _trs;

        [SerializeField] private float sensitivity = 100f;
        [SerializeField] private float pitchMin = -80;
        [SerializeField] private float pitchMax = 80;
        private float pitch;
        private float yaw;

        private Vector2 lookVector;

        [SerializeField] private CinemachineThirdPersonFollow thirdCamera;
        [SerializeField] private float camDistance = 8.0f;
        [SerializeField] private Renderer player;
        private bool isThirdPerson = false;
        float currentVelocity;

        private IDisposable _bag;
        private bool curCamState = true;
        private bool _vehicleMode;
        private Transform _originalParent;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            pitch = _trs.eulerAngles.x;
            yaw = _trs.eulerAngles.y;
        }

        private void Locker(PlayerUIStateMsg msg)
        {
            switch (msg.state)
            {
                case PlayerUIState.Inventory:
                    Cursor.lockState = CursorLockMode.None;
                    curCamState = false;
                    break;
                default:
                    Cursor.lockState = CursorLockMode.Locked;
                    curCamState = true;
                    break;
            }
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (!curCamState || _vehicleMode) return;

            lookVector = context.ReadValue<Vector2>();

            pitch -= lookVector.y * sensitivity;
            yaw += lookVector.x * sensitivity;
        }

        private void OnVehicleControlAssigned(VehicleControlAssignedMsg msg)
        {
            if (msg.Controller != null)
            {
                _originalParent = _trs.parent;
                _trs.SetParent(msg.Controller.CameraAnchor);
                _trs.localPosition = Vector3.zero;
                _trs.localRotation = Quaternion.identity;
                _vehicleMode = true;
            }
            else
            {
                _trs.SetParent(_originalParent);
                pitch = _trs.eulerAngles.x;
                yaw = _trs.eulerAngles.y;
                _vehicleMode = false;
            }
        }

        public void OnPeronChange(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<float>();

            if (value > 0.1f)
            {
                isThirdPerson = true;
            }
            else if (value < -0.1f)
            {
                isThirdPerson = false;
            }
        }

        private void LateUpdate()
        {
            if (!curCamState) return;

            if (!_vehicleMode)
            {
                pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
                _trs.rotation = Quaternion.Euler(pitch, yaw, 0);
            }

            float targetDistance = isThirdPerson ? camDistance : 0;

            thirdCamera.CameraDistance =
                Mathf.SmoothDamp(thirdCamera.CameraDistance, targetDistance, ref currentVelocity, 0.3f);


            if (Mathf.Approximately(thirdCamera.CameraDistance, targetDistance))
            {
                thirdCamera.CameraDistance = targetDistance;
                currentVelocity = 0f;
            }

            player.enabled = thirdCamera.CameraDistance >= 0.5f;
        }

        [Inject]
        private void Constructor(
            ISubscriber<PlayerUIStateMsg> iPlayerUIStateSub,
            ISubscriber<VehicleControlAssignedMsg> vehicleControlSub)
        {
            var builder = DisposableBag.CreateBuilder();

            iPlayerUIStateSub.Subscribe(Locker).AddTo(builder);
            vehicleControlSub.Subscribe(OnVehicleControlAssigned).AddTo(builder);

            _bag = builder.Build();
        }

        private void OnDestroy() => _bag?.Dispose();
    }
}
