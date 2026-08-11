using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Structure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Entity), typeof(BoxCollider))]
    public sealed class CollectibleLog : EntityFeature, IInteractionTarget, IInteractionPrompt
    {
        [SerializeField] private string logId = "sample-log-01";
        [SerializeField] private Vector3 hologramOffset = new(0f, 1.05f, 0f);

        private ILogCollectionWriter _collection;
        private ILogCatalog _catalog;
        private INotificationService _notifications;
        private WorldLogHologram _hologram;
        private LogEntry _entry;

        protected override void Awake()
        {
            base.Awake();
            EnsureHologram();
            Owner.Configure(logId, logId, EntityKind.WorldItem);
        }

        [Inject]
        public void Construct(ILogCollectionWriter collection, ILogCatalog catalog,
            INotificationService notifications)
        {
            BindToEntity();
            EnsureHologram();
            _collection = collection;
            _catalog = catalog;
            _notifications = notifications;
            _entry = _catalog.Get(logId);
            if (_entry == null)
            {
                Debug.LogError($"[CollectibleLog] Unknown log id: {logId}", this);
                return;
            }
            Owner?.Configure(_entry.id, _entry.title, EntityKind.WorldItem);
            _hologram?.Configure(_entry.image);
        }

        public void Interact()
        {
            if (_entry == null || _collection == null || !_collection.Add(_entry)) return;
            _notifications?.Show("로그 수집", _entry.title, "▣", NotificationKind.Info, 3.5f);
            Destroy(Owner.gameObject);
        }

        public string GetLabel() => _entry != null ? $"로그 수집: {_entry.title}" : $"로그 수집: {logId}";
        public string GetPromptKey() => "F";

        private void EnsureHologram()
        {
            _hologram = GetComponentInChildren<WorldLogHologram>(true);
            if (!_hologram)
            {
                Debug.LogWarning("[CollectibleLog] Scene-authored Log Hologram is missing.", this);
                return;
            }

            _hologram.transform.localPosition = hologramOffset;
            _hologram.Configure(_entry?.image);
        }
    }
}
