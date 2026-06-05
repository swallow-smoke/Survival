using System;
using _001_Scripts.Data.Item;
using _001_Scripts.Data.Message;
using _001_Scripts.Type.Item;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _001_Scripts.UI.Component
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private string itemName;
        [SerializeField] private string itemDesc;
        [SerializeField] private string itemType;
        [SerializeField] private TextMeshProUGUI stock;
        [SerializeField] private Image durability;
        [SerializeField] private int index;

        private CanvasGroup _canvasGroup;
        private IPublisher<InvSwapMessage> _publisher;

        public static ItemSlot DragSlot;

        private bool isCanDrop;
        private Vector2 originPos;
        
        public void Set(InventorySlot slot, Template template, int index, IPublisher<InvSwapMessage> publisher)
        {
            itemName = template.itemName;
            itemDesc = template.itemDesc;
            itemType = template.itemType.ToString();
            this.index = index;
            _publisher = publisher;

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
            
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originPos = eventData.position;
            DragSlot = this;
            _canvasGroup.blocksRaycasts = false;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            DragSlot = null;
            _canvasGroup.blocksRaycasts = true;

            if (!isCanDrop)
            {
                this.transform.position = originPos;
            }
            
            isCanDrop = false;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var slot = eventData.pointerDrag.GetComponent<ItemSlot>();
            isCanDrop = true;
            
            _publisher.Publish(new InvSwapMessage(slot.index, index));
        }
    }
}