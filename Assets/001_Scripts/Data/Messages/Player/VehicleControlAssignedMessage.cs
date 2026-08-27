using AstraNope.Contracts.WorldObjects;

namespace AstraNope.Data.Messages.Player
{
    public readonly struct VehicleControlAssignedMessage
    {
        public readonly IVehicleControllable Controller; // null = 조종 해제
        public readonly ISeat Seat;                      // null = 조종 해제

        public VehicleControlAssignedMessage(IVehicleControllable controller, ISeat seat)
        {
            Controller = controller;
            Seat = seat;
        }
    }
}
