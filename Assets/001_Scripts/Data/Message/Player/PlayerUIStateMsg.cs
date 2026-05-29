using _001_Scripts.Type.States;

namespace _001_Scripts.Data.Message.Player
{
    public readonly struct PlayerUIStateMsg
    {
        public readonly PlayerUIState state;

        public PlayerUIStateMsg(PlayerUIState state) => this.state = state;
    }
}