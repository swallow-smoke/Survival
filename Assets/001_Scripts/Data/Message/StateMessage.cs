using _001_Scripts.Type;

namespace MessagePipe
{
    public readonly struct StateMessage
    {
        public readonly PlayerState state;

        public StateMessage(PlayerState state)
        {
            this.state = state;
        }
    }
}