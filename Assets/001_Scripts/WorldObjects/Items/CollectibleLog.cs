using AstraNope.Data;
using AstraNope.Data.Messages;
using AstraNope.Contracts.WorldObjects;
using AstraNope.WorldObjects.Entities;
using AstraNope.Contracts;
using UnityEngine;
using VContainer;

using AstraNope.Localization;
namespace AstraNope.WorldObjects.Items
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
            _notifications?.Show(L10n.T("k_9f5e554890"), _entry.title, "▣", NotificationKind.Info, 3.5f);
            Destroy(Owner.gameObject);
        }

        public string GetLabel() => _entry != null ? L10n.F("k_0ccc534597", _entry.title) : L10n.F("k_0ccc534597", logId);
        public string GetPromptKey() => "LMB";

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
