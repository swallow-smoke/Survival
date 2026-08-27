using System.Collections.Generic;
using UnityEngine;

namespace AstraNope.WorldObjects.Entities
{
    /// <summary>Composition root that discovers capability components on itself and its children.</summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class Entity : MonoBehaviour, IEntity
    {
        [SerializeField] private string entityId;
        [SerializeField] private string displayName;
        [SerializeField] private EntityKind kind;

        private readonly List<MonoBehaviour> _components = new();

        public string EntityId => string.IsNullOrWhiteSpace(entityId) ? gameObject.name : entityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public EntityKind Kind => kind;

        private void Awake() => RebuildFeatures();
        private void OnTransformChildrenChanged() => RebuildFeatures();

        public void Configure(string id, string label, EntityKind entityKind)
        {
            entityId = id;
            displayName = label;
            kind = entityKind;
        }

        public void SetKind(EntityKind entityKind) => kind = entityKind;

        public void RebuildFeatures()
        {
            _components.Clear();
            var components = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var component in components)
            {
                if (!component || component == this) continue;
                if (component.GetComponentInParent<Entity>(true) != this) continue;
                _components.Add(component);
            }
        }

        internal void Register(MonoBehaviour feature)
        {
            if (feature && !_components.Contains(feature)) _components.Add(feature);
        }

        internal void Unregister(MonoBehaviour feature) => _components.Remove(feature);

        public bool TryGetFeature<T>(out T feature) where T : class
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T match)
                {
                    feature = match;
                    return true;
                }
            }

            RebuildFeatures();
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T match)
                {
                    feature = match;
                    return true;
                }
            }

            feature = null;
            return false;
        }

        public IReadOnlyList<T> GetFeatures<T>() where T : class
        {
            RebuildFeatures();
            var result = new List<T>();
            foreach (var component in _components)
                if (component is T match) result.Add(match);
            return result;
        }
    }
}
