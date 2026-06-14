using _001_Scripts.Type.States;

namespace _001_Scripts.Data.Structure.Interface
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
