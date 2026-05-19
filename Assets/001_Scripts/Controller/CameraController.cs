using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts.Controller
{
    // why cinemachine built-in cam controller is not working for 3.6.1;;
    // so i made this.
    // damper is not valid for this one, so i'll make it later;
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public Transform _trs;

        [SerializeField] private float sensitivity = 100f;
        [SerializeField] private float pitchMin = -80;
        [SerializeField] private float pitchMax = 80;
        private float pitch;
        private float yaw;

        private Vector2 lookVector;

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

        private void LateUpdate()
        {
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            _trs.rotation = Quaternion.Euler(pitch, yaw, 0);
        }
    }
}