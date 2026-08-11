using UnityEngine;

namespace _001_Scripts.Object.Vehicle
{
    public interface ICameraAnchored
    {
        Transform CameraAnchor { get; }
    }

    public interface IControlLifecycle
    {
        void EnterControl();
        void ExitControl();
    }

    public interface IVehicleMotionController
    {
        void HandleMove(Vector2 wasd);
        void HandleLook(Vector2 mouseDelta);
        void HandleVertical(float value);
    }

    public interface IVehicleControllable : ICameraAnchored, IControlLifecycle, IVehicleMotionController { }
}
