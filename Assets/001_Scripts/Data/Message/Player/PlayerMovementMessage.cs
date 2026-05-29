using UnityEngine;

namespace _001_Scripts.Data.Message
{
    public readonly struct PlayerMovementMessage
    {
        public readonly float velocity;
        public readonly bool isGround;
        public readonly Vector3 rawVector3;

        public PlayerMovementMessage(float velocity, bool isGround, Vector3 rawVector3)
        {
            this.velocity = velocity;
            this.isGround = isGround;
            this.rawVector3 = rawVector3;
        }
    }
}