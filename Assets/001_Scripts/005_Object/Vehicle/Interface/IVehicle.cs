using _001_Scripts.Type.States;

namespace _001_Scripts.Object.Vehicle
{
    public interface IVehicle
    {
        float Fuel { get; }
        float MaxFuel { get; }
        VehicleConditionState Condition { get; }
        bool ConsumeFuel(float amount);
        void Repair(float amount);
    }
}
