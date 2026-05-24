using _001_Scripts.Data.Message;
using Unity.Mathematics;
using UnityEngine;

namespace _001_Scripts
{
    public static class Util
    {
        public static bool HasSignificantChange(PlayerStatMessage current, PlayerStatMessage previous)
        {
            bool result = false;
            float max = 1f;

            if (Mathf.Abs(current.stamina - previous.stamina) >= max) result = true;
            if (Mathf.Abs(current.hungry - previous.hungry) >= max) result = true;
            if (Mathf.Abs(current.water - previous.water) >= max) result = true;
            if (Mathf.Abs(current.oxygen - previous.oxygen) >= max) result = true;
            if (current.hp - previous.hp >= max) result = true;
            
            return result;
        }
    }
}