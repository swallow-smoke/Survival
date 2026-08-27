namespace AstraNope.Gameplay.Movement
{
    public interface IMovementMode
    {
        void Tick(MovementContext ctx);
    }
}
