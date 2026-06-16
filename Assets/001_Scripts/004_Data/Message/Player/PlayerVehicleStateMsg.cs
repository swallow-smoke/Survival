using _001_Scripts.Type.States;

namespace _001_Scripts.Data.Message
{
    public readonly struct PlayerVehicleStateMsg
    {
        public readonly PlayerVehicleState state;

        public PlayerVehicleStateMsg(PlayerVehicleState state) => this.state = state;
    }
}