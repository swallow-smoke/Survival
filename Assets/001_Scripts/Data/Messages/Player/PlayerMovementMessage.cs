using UnityEngine;

namespace AstraNope.Data.Messages
{
    public readonly struct PlayerMovementMessage
    {
        public readonly float velocity;
        public readonly bool isGround;
        public readonly Vector3 rawVector3;
        public readonly bool isSwimming;

        public PlayerMovementMessage(float velocity, bool isGround, Vector3 rawVector3, bool isSwimming)
        {
            this.velocity = velocity;
            this.isGround = isGround;
            this.rawVector3 = rawVector3;
            this.isSwimming = isSwimming;
        }
    }
}