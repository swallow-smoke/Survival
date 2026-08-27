using UnityEngine;
using UnityEngine.Serialization;

namespace AstraNope.WorldObjects.Entities
{
    public interface IWorldItem
    {
        int ItemId { get; }
        int Count { get; }
        void Setup(int id, int amount, string displayName = null);
    }

    [DisallowMultipleComponent]
    public sealed class WorldItem : EntityFeature, IWorldItem
    {
        [FormerlySerializedAs("dropItemId")]
        [SerializeField] private int itemId;
        [FormerlySerializedAs("dropCount")]
        [SerializeField, Min(1)] private int count = 1;

        public int ItemId => itemId;
        public int Count => count;

        protected override void Awake()
        {
            base.Awake();
            Owner.SetKind(EntityKind.WorldItem);
        }

        public void Setup(int id, int amount, string displayName = null)
        {
            itemId = id;
            count = Mathf.Max(1, amount);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                Owner.Configure(Owner.EntityId, displayName, EntityKind.WorldItem);
                gameObject.name = displayName;
            }
        }
    }
}
