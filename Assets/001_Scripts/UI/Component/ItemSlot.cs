using _001_Scripts.Data.Item;
using _001_Scripts.Type.Item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    public class ItemSlot : MonoBehaviour //, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private TextMeshProUGUI itemDesc;
        [SerializeField] private TextMeshProUGUI itemType;
        [SerializeField] private TextMeshProUGUI stock;
        [SerializeField] private Image durability;

        private Vector2 originPos;
        
        
        public void Set(InventorySlotData data)
        {
            itemName.text = data.item.itemName;
            itemDesc.text = data.item.itemDesc;
            itemType.text = data.item.itemType.ToString();
            stock.text = data.count.ToString();

            if (data.item.HasAttributes(ItemAttributesType.Stackable))
            {
                durability.fillAmount = data.item.durability;
                durability.gameObject.SetActive(true);
            }
            else
            {
                durability.gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            gameObject.SetActive(false);
        }
        // currently working
        // public void OnBeginDrag(PointerEventData eventData)
        // {
        //     originPos = eventData.position;
        // }
        //
        // public void OnDrag(PointerEventData eventData)
        // {
        //     transform.position = eventData.position;
        // }
        //
        // public void OnEndDrag(PointerEventData eventData)
        // {
        //     
        // }
    }
}