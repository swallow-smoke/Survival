using _001_Scripts.Type;

namespace MessagePipe
{
    public readonly struct PlayerStateMessage
    {
        public readonly PlayerState state;

        public PlayerStateMessage(PlayerState state)
        {
            this.state = state;
        }
    }
}