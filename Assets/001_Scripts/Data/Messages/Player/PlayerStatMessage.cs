namespace AstraNope.Data.Messages
{
    public readonly struct PlayerStatMessage
    {
        public readonly int hp;
        public readonly float stamina;
        public readonly float hungry;
        public readonly float water;
        public readonly float oxygen;
        public readonly float temp;

        public PlayerStatMessage(int hp, 
            float stamina, 
            float hungry, 
            float water, 
            float oxygen,
            float temp)
        {
            this.hp = hp;
            this.hungry = hungry;
            this.water = water;
            this.oxygen = oxygen;
            this.stamina = stamina;
            this.temp = temp;
        }
    }
}