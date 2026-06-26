using UnityEngine;

namespace _001_Scripts.Controller.Movement
{
    public class SwimMovementMode : IMovementMode
    {
        public void Tick(MovementContext ctx)
        {
            float verticalVelocity = 0f;
            if (ctx.IsSwimUp) verticalVelocity = ctx.SwimVerticalSpeed;
            else if (ctx.IsSwimDown) verticalVelocity = -ctx.SwimVerticalSpeed;

            ctx.Rb.linearVelocity = new Vector3(ctx.MoveDir.x * ctx.SwimSpeed, verticalVelocity, ctx.MoveDir.z * ctx.SwimSpeed);
        }
    }
}
