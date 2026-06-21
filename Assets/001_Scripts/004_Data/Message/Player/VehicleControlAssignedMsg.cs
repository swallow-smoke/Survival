using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Object.Vehicle;

namespace _001_Scripts.Data.Message.Player
{
    public readonly struct VehicleControlAssignedMsg
    {
        public readonly IVehicleControllable Controller; // null = 조종 해제
        public readonly ISeat Seat;                      // null = 조종 해제

        public VehicleControlAssignedMsg(IVehicleControllable controller, ISeat seat)
        {
            Controller = controller;
            Seat = seat;
        }
    }
}
