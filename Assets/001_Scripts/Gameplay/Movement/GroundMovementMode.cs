using UnityEngine;

namespace AstraNope.Gameplay.Movement
{
    public class GroundMovementMode : IMovementMode
    {
        public void Tick(MovementContext ctx)
        {
            float currentSpeed = ctx.IsCrouching ? ctx.CrouchSpeed : (ctx.IsRunning ? ctx.RunningSpeed : ctx.Speed);
            var rb = ctx.Rb;
            rb.linearVelocity = new Vector3(ctx.MoveDir.x * currentSpeed, rb.linearVelocity.y, ctx.MoveDir.z * currentSpeed);
        }
    }
}
