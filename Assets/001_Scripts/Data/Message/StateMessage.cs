using _001_Scripts.Type;

namespace _001_Scripts.Data.Message
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