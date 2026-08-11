using _001_Scripts.Entities;
using _001_Scripts.Type.States;

namespace _001_Scripts.Object.Vehicle
{
    public interface IFuelPowered
    {
        float Fuel { get; }
        float MaxFuel { get; }
        bool ConsumeFuel(float amount);
        void Refuel(float amount);
    }

    public interface IVehicleCondition
    {
        VehicleConditionState Condition { get; }
    }

    public interface IVehicle : IFuelPowered, IVehicleCondition, IRepairable
    {
        void Repair(float amount);
    }
}
