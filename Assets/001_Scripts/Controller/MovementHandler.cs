using UnityEngine;

namespace _001_Scripts.Controller
{
    public class MovementHandler : MonoBehaviour
    {
        public Vector2 Dir { get; private set; }

        public void Update()
        {
            Dir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }
    }
}