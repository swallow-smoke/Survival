using AstraNope.Types.States;

namespace AstraNope.Data.Messages
{
    public readonly struct PlayerVehicleStateMessage
    {
        public readonly PlayerVehicleState state;

        public PlayerVehicleStateMessage(PlayerVehicleState state) => this.state = state;
    }
}