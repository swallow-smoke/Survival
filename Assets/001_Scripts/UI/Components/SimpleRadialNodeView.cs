using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AstraNope.UI.Components
{
    [DisallowMultipleComponent]
    public sealed class SimpleRadialNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private RadialCircleGraphic circle;
        [SerializeField] private Button button;
        [SerializeField] private Text content;

        private SimpleRadialMenuView _owner;
        private Action _primary;
        private Action _secondary;
        private string _tooltip;
        private SimpleRadialRecipeTooltipData _recipeTooltip;

        public void BindSerializedReferences(RadialCircleGraphic nodeCircle, Button nodeButton, Text nodeContent)
        {
            circle = nodeCircle;
            button = nodeButton;
            content = nodeContent;
            BindPrimary();
        }

        public void Configure(SimpleRadialEntry entry, Vector2 position, SimpleRadialMenuView owner,
            Color enabledColor, Color disabledColor)
        {
            _owner = owner;
            _primary = entry.Selected;
            _secondary = entry.SecondarySelected;
            _tooltip = entry.Tooltip;
            _recipeTooltip = entry.RecipeTooltip;
            gameObject.name = string.IsNullOrWhiteSpace(entry.Id) ? "Node" : entry.Id;
            ((RectTransform)transform).anchoredPosition = position;
            if (circle) circle.color = entry.Interactable ? enabledColor : disabledColor;
            if (button) button.interactable = entry.Interactable;
            if (content)
                content.text = string.IsNullOrWhiteSpace(entry.Label)
                    ? entry.Icon
                    : $"{entry.Icon}\n{entry.Label}";
            BindPrimary();
            gameObject.SetActive(true);
        }

        public void Clear()
        {
            _primary = null;
            _secondary = null;
            _tooltip = null;
            _recipeTooltip = null;
            gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_recipeTooltip != null) _owner?.ShowRecipeTooltip(_recipeTooltip, eventData.position);
            else if (!string.IsNullOrWhiteSpace(_tooltip)) _owner?.ShowTooltip(_tooltip, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData) => _owner?.HideTooltip();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || _secondary == null) return;
            _secondary.Invoke();
            eventData.Use();
        }

        private void BindPrimary()
        {
            if (!button) return;
            button.onClick.RemoveListener(InvokePrimary);
            button.onClick.AddListener(InvokePrimary);
        }

        private void InvokePrimary() => _primary?.Invoke();
    }
}
