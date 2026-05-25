using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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


        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookVector = context.ReadValue<Vector2>();

            pitch -= lookVector.y * sensitivity;
            yaw += lookVector.x * sensitivity;
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
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            _trs.rotation = Quaternion.Euler(pitch, yaw, 0);

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
    }
}