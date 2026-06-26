using UnityEngine;

namespace _001_Scripts.Controller.Movement
{
    public class InsideLargeMovementMode : IMovementMode
    {
        public void Tick(MovementContext ctx)
        {
            float currentSpeed = ctx.IsCrouching ? ctx.CrouchSpeed : (ctx.IsRunning ? ctx.RunningSpeed : ctx.Speed);
            var rb = ctx.Rb;
            rb.MovePosition(rb.position + ctx.MoveDir * (currentSpeed * Time.fixedDeltaTime));
        }
    }
}
