using UnityEngine;

namespace AstraNope.WorldObjects.Entities
{
    public abstract class EntityFeature : MonoBehaviour
    {
        public Entity Owner { get; private set; }

        protected virtual void Awake() => BindToEntity();
        protected virtual void OnEnable() => BindToEntity();

        protected virtual void OnDisable()
        {
            if (Owner) Owner.Unregister(this);
        }

        protected virtual void OnTransformParentChanged() => BindToEntity();

        protected void BindToEntity()
        {
            var owner = GetComponentInParent<Entity>(true);
            if (!owner) owner = gameObject.AddComponent<Entity>();
            if (Owner == owner)
            {
                Owner.Register(this);
                return;
            }

            if (Owner) Owner.Unregister(this);
            Owner = owner;
            Owner.Register(this);
        }

        protected bool TryGetFeature<T>(out T feature) where T : class
        {
            if (Owner) return Owner.TryGetFeature(out feature);
            feature = null;
            return false;
        }
    }
}
