using UnityEngine;

namespace _001_Scripts.Core
{
    public class BootStrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Boot()
        {
            
        }

        public static void InitializeGame()
        {
            
        }
    }
}