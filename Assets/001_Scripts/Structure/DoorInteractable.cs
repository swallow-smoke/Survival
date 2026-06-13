using _001_Scripts.Data.Structure.Interface;
using UnityEngine;

namespace _001_Scripts.Structure
{
    public class DoorInteractable : MonoBehaviour, IInteractable, IInteractableInfo
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string openParam = "IsOpen";

        private bool isOpen;

        public void Interact()
        {
            isOpen = !isOpen;
            animator.SetBool(openParam, isOpen);
        }

        public string GetLabel() => isOpen ? "Close Door" : "Open Door";
    }
}
