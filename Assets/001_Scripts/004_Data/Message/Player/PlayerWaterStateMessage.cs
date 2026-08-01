using _001_Scripts.Core._000_World._001_Water;

namespace _001_Scripts.Data.Message.Player
{
    public readonly struct PlayerWaterStateMessage
    {
        public readonly PlayerWaterState State;

        public PlayerWaterStateMessage(PlayerWaterState state) => State = state;
    }
}
