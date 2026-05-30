using _001_Scripts.Type.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI itemDesc;
        [SerializeField] private TextMeshProUGUI itemType;
        
        
        public void Set(string itemName, string itemDesc, ItemType type)
        {
            
        }
    }
}