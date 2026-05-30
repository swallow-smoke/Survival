using _001_Scripts.Data.Item;
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
        [SerializeField] private TextMeshProUGUI stock;
        
        
        public void Set(InventorySlotData data)
        {
            itemName.text = data.item.itemName;
            itemDesc.text = data.item.itemDesc;
            itemType.text = data.item.itemType.ToString();
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }
    }
}