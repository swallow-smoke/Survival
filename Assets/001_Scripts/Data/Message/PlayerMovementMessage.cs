namespace _001_Scripts.Data.Message
{
    public readonly struct PlayerMovementMessage
    {
        public readonly float velocity;
        public readonly bool isRunning;

        public PlayerMovementMessage(float velocity, bool isRunning)
        {
            this.velocity = velocity;
            this.isRunning = isRunning;
        }
    }
}