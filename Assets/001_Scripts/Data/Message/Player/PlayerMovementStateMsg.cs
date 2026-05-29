using _001_Scripts.Type.States;

namespace _001_Scripts.Data.Message.Player
{
    public readonly struct PlayerMovementStateMsg
    {
        public readonly PlayerMovementState movement;
        
        public PlayerMovementStateMsg (PlayerMovementState movement) => this.movement = movement;
    }
}