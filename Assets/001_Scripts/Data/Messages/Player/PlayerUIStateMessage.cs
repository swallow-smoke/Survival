using AstraNope.Types.States;

namespace AstraNope.Data.Messages.Player
{
    public readonly struct PlayerUIStateMessage
    {
        public readonly PlayerUIState state;

        public PlayerUIStateMessage(PlayerUIState state) => this.state = state;
    }
}