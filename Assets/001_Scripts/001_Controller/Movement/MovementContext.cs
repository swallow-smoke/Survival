using UnityEngine;

namespace _001_Scripts.Controller.Movement
{
    public class MovementContext
    {
        public Rigidbody Rb;

        public float Speed;
        public float RunningSpeed;
        public float CrouchSpeed;
        public float SwimSpeed;
        public float SwimVerticalSpeed;

        public Vector3 MoveDir;
        public bool IsRunning;
        public bool IsCrouching;
        public bool IsSwimUp;
        public bool IsSwimDown;
    }
}
