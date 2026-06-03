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
        [SerializeField] private string itemName;
        [SerializeField] private string itemDesc;
        [SerializeField] private string itemType;
        [SerializeField] private TextMeshProUGUI stock;
        [SerializeField] private Image durability;

        private Vector2 originPos;
        
        public void Set(InventorySlot slot, Template template)
        {
            itemName = template.itemName;
            itemDesc = template.itemDesc;
            itemType = template.itemType.ToString();

            if (template.HasAttribute(AttributesType.Equippable))
            {
                durability.fillAmount =
                    slot.ins.durability /
                    template.GetModifierValue(AttributesType.Equippable, ModifierType.DurabilityMax);
                stock.gameObject.SetActive(false);
                durability.gameObject.SetActive(true);
            }
            else
            {
                stock.text = slot.stack.ToString();
                durability.gameObject.SetActive(false);
                stock.gameObject.SetActive(true);
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