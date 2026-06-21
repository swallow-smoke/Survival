using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Object.Vehicle;
using _001_Scripts.Type.States;
using UnityEngine;

namespace _001_Scripts.Vehicle.Core
{
    public abstract class VehicleBody : MonoBehaviour, IVehicle
    {
        [SerializeField] protected float fuel;
        [SerializeField] protected float maxFuel;
        [SerializeField] protected VehicleConditionState condition;

        public float Fuel => fuel;
        public float MaxFuel => maxFuel;
        public VehicleConditionState Condition => condition;

        public bool ConsumeFuel(float amount)
        {
            if (fuel <= 0f) return false;
            fuel = Mathf.Max(0f, fuel - amount);
            return true;
        }

        public void Repair(float amount) { }
    }
}
