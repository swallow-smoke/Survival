using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Object.Item
{
    public enum PrototypeHeldItemKind
    {
        Scanner,
        HarvestTool,
        Flashlight,
        OxygenTank,
        BuildTool
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeHeldItemAction : MonoBehaviour, IHeldItemAction
    {
        [SerializeField] private PrototypeHeldItemKind kind;
        [SerializeField] private Light flashlight;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color idleEmission = new(.08f, .35f, .55f, 1f);
        [SerializeField] private Color activeEmission = new(.25f, 1f, 1f, 1f);

        private MaterialPropertyBlock _properties;
        private float _pulseUntil;
        private bool _flashlightEnabled;

        public void Configure(PrototypeHeldItemKind itemKind, Light itemLight, Renderer itemIndicator)
        {
            kind = itemKind;
            flashlight = itemLight;
            indicator = itemIndicator;
        }

        public void OnEquipped(_001_Scripts.Data.Item.Item item, Instance instance)
        {
            _flashlightEnabled = false;
            if (flashlight) flashlight.enabled = false;
            SetIndicator(idleEmission);
        }

        public bool TryPerformPrimaryAction(_001_Scripts.Data.Item.Item item, Instance instance)
        {
            if (kind == PrototypeHeldItemKind.Flashlight && flashlight)
            {
                _flashlightEnabled = !_flashlightEnabled;
                flashlight.enabled = _flashlightEnabled;
                SetIndicator(_flashlightEnabled ? activeEmission : idleEmission);
                return true;
            }

            _pulseUntil = Time.unscaledTime + .22f;
            SetIndicator(activeEmission);
            return true;
        }

        private void Update()
        {
            if (_pulseUntil <= 0f || Time.unscaledTime < _pulseUntil) return;
            _pulseUntil = 0f;
            SetIndicator(idleEmission);
        }

        private void SetIndicator(Color color)
        {
            if (!indicator) return;
            _properties ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _properties.SetColor("_EmissionColor", color * 3f);
            indicator.SetPropertyBlock(_properties);
        }
    }
}
