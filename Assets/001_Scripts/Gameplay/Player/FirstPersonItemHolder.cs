using AstraNope.Data.Items;
using AstraNope.Contracts;
using UnityEngine;

namespace AstraNope.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class FirstPersonItemHolder : MonoBehaviour, IItemHolder
    {
        public const string EquipTrigger = "Equip";
        public const string UseTrigger = "Use";

        [SerializeField, Tooltip("Item view prefabs are parented here. Defaults to this transform.")]
        private Transform mount;

        [SerializeField, Tooltip("Optional scene-authored procedural motion rig.")]
        private FirstPersonItemMotion motion;

        [Header("Held View")]
        [SerializeField] private bool disableColliders = true;
        [SerializeField] private bool disableRigidbodyCollisions = true;
        [SerializeField, Range(-1, 31), Tooltip("-1 keeps prefab layers unchanged.")]
        private int overrideLayer = -1;

        public Transform Mount => mount ? mount : transform;
        public Item HeldItem { get; private set; }
        public ItemInstance HeldInstance { get; private set; }
        public GameObject HeldObject { get; private set; }
        public bool IsHolding => HeldObject;

        public bool TryEquip(Item item, ItemInstance instance = null)
        {
            if (item == null || !item.TryGetFeature<IHoldable>(out var holdable) || !holdable.FirstPersonPrefab)
                return false;

            return TryEquip(holdable.FirstPersonPrefab, item, instance);
        }

        public bool TryEquip(GameObject viewPrefab, Item item = null, ItemInstance instance = null)
        {
            if (!viewPrefab) return false;

            ClearHeldObject();
            HeldItem = item;
            HeldInstance = instance;
            HeldObject = Instantiate(viewPrefab, Mount, false);
            HeldObject.name = viewPrefab.name;

            var heldTransform = HeldObject.transform;
            heldTransform.localPosition = Vector3.zero;
            heldTransform.localRotation = Quaternion.identity;

            PrepareView(HeldObject);
            InvokeEquippedHooks();
            TrySetTrigger(EquipTrigger);
            ResolveMotion()?.PlayEquip();
            return true;
        }

        public bool TryPerformPrimaryAction()
        {
            if (!HeldObject) return false;
            bool handled = PlayMotion(FirstPersonItemAction.Use) | TrySetTrigger(UseTrigger);
            foreach (var behaviour in HeldObject.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IHeldItemAction action)
                    handled |= action.TryPerformPrimaryAction(HeldItem, HeldInstance);
            return handled;
        }

        public bool TryPerformHarvestAction()
        {
            if (!HeldObject || HeldItem == null || !HeldItem.HasFeature<ITool>()) return false;
            bool handled = PlayMotion(FirstPersonItemAction.Harvest) | TrySetTrigger(UseTrigger);
            foreach (var behaviour in HeldObject.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IHeldItemAction action)
                    handled |= action.TryPerformPrimaryAction(HeldItem, HeldInstance);
            return handled;
        }

        public void Unequip()
        {
            if (!HeldObject) return;
            FirstPersonItemMotion resolved = ResolveMotion();
            if (Application.isPlaying && resolved)
                resolved.PlayUnequip(ClearHeldObject);
            else
                ClearHeldObject();
        }

        public void Configure(Transform itemMount, FirstPersonItemMotion itemMotion)
        {
            mount = itemMount;
            motion = itemMotion;
        }

        private FirstPersonItemMotion ResolveMotion()
        {
            if (!motion && Mount)
                motion = Mount.GetComponentInParent<FirstPersonItemMotion>();
            return motion;
        }

        private bool PlayMotion(FirstPersonItemAction action)
        {
            FirstPersonItemMotion resolved = ResolveMotion();
            if (!resolved) return false;
            resolved.Play(action);
            return true;
        }

        private void ClearHeldObject()
        {
            ResolveMotion()?.Cancel(resetPose: true);
            if (HeldObject)
            {
                if (Application.isPlaying) Destroy(HeldObject);
                else DestroyImmediate(HeldObject);
            }
            HeldObject = null;
            HeldItem = null;
            HeldInstance = null;
        }

        private void PrepareView(GameObject root)
        {
            if (overrideLayer >= 0) SetLayerRecursively(root.transform, overrideLayer);

            if (disableColliders)
                foreach (var itemCollider in root.GetComponentsInChildren<Collider>(true))
                    itemCollider.enabled = false;

            if (!disableRigidbodyCollisions) return;
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void InvokeEquippedHooks()
        {
            foreach (var behaviour in HeldObject.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IHeldItemAction action)
                    action.OnEquipped(HeldItem, HeldInstance);
        }

        private bool TrySetTrigger(string triggerName)
        {
            var animator = HeldObject ? HeldObject.GetComponentInChildren<Animator>(true) : null;
            if (!animator || !animator.runtimeAnimatorController) return false;
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Trigger || parameter.name != triggerName) continue;
                animator.SetTrigger(triggerName);
                return true;
            }
            return false;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
