using UnityEngine;

namespace _001_Scripts.Object.Vehicle
{
    public interface IVehicleControllable
    {
        Transform CameraAnchor { get; }
        void EnterControl();
        void ExitControl();
        void HandleMove(Vector2 wasd);
        void HandleLook(Vector2 mouseDelta);
        void HandleVertical(float value);
    }
}
