using AstraNope.WorldObjects.Entities;
using AstraNope.Types.States;

namespace AstraNope.WorldObjects.Vehicles
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
