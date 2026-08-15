using System.Collections.Generic;
using _001_Scripts.Data.BluePrint;
using _001_Scripts.Data.Building;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Data.Structure.Interface;
using _001_Scripts.Interface;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _001_Scripts.Controller
{
    [DisallowMultipleComponent]
    public sealed class BuildingPlacementController : MonoBehaviour, IBuildingPlacementService, IBuildSelectionReader
    {
        [SerializeField, Tooltip("Usually the first-person camera transform.")]
        private Transform view;
        [SerializeField] private LayerMask placementSurfaceMask = ~0;
        [SerializeField] private LayerMask blockingMask = ~0;
        [SerializeField] private List<BuildingDefinition> definitions = new();

        [Header("Hologram")]
        [SerializeField] private Color validColor = new(.12f, 1f, .86f, .48f);
        [SerializeField] private Color invalidColor = new(1f, .18f, .38f, .48f);
        [SerializeField, Min(0f)] private float emissionStrength = 2.4f;

        private IInventoryService _inventory;
        private BluePrintDataBase _blueprints;
        private IBlueprintProgressReader _progress;
        private INotificationService _notifications;
        private IPublisher<InteractionUIMessage> _interactionUi;
        private BuildingDefinition _active;
        private GameObject _preview;
        private Renderer[] _previewRenderers;
        private MaterialPropertyBlock _properties;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _candidatePosition;
        private bool _canPlace;
        private int _beganFrame = -1;
        private int _lastBlueprintId = -1;
        private readonly Collider[] _overlapBuffer = new Collider[16];
        private string _validMessage;
        private string _invalidMessage;
        private bool _publishedCanPlace;
        private bool _hasPublishedState;

        public bool IsPlacing => _active != null && _preview;
        public int ActiveBlueprintId => _active?.blueprintId ?? -1;
        public int LastBlueprintId => _lastBlueprintId;

        [Inject]
        private void Construct(IInventoryService inventory, BluePrintDataBase blueprints,
            IBlueprintProgressReader progress, INotificationService notifications,
            IPublisher<InteractionUIMessage> interactionUi)
        {
            _inventory = inventory;
            _blueprints = blueprints;
            _progress = progress;
            _notifications = notifications;
            _interactionUi = interactionUi;
        }

        public void Configure(Transform viewTransform, List<BuildingDefinition> buildingDefinitions)
        {
            view = viewTransform;
            definitions = buildingDefinitions ?? new List<BuildingDefinition>();
        }

        public bool TryBegin(int blueprintId, out string failureReason)
        {
            failureReason = string.Empty;
            BuildingDefinition definition = definitions?.Find(candidate =>
                candidate != null && candidate.blueprintId == blueprintId);
            if (definition == null || !definition.structurePrefab || !definition.previewPrefab)
            {
                failureReason = "배치 가능한 건축물 청사진이 아닙니다.";
                return false;
            }

            if (_progress == null || !_progress.TryGetBlueprint(blueprintId, out var status) || !status.IsUnlocked)
            {
                failureReason = "아직 청사진이 해금되지 않았습니다.";
                return false;
            }

            Cancel();
            _active = definition;
            _lastBlueprintId = definition.blueprintId;
            _rotation = Quaternion.identity;
            _preview = Instantiate(definition.previewPrefab);
            _preview.name = $"{definition.DisplayName} [Placement Preview]";
            _previewRenderers = _preview.GetComponentsInChildren<Renderer>(true);
            DisablePreviewPhysics(_preview);
            _validMessage = $"{definition.DisplayName} 설치 가능";
            _invalidMessage = $"{definition.DisplayName} 설치 불가";
            _hasPublishedState = false;
            _beganFrame = Time.frameCount;
            UpdateCandidate();
            _notifications?.Show("건축 배치", "LMB 설치  ·  R 회전  ·  RMB 취소", "◇",
                NotificationKind.Info, 4f);
            return true;
        }

        public void Cancel()
        {
            if (_preview)
            {
                if (Application.isPlaying) Destroy(_preview);
                else DestroyImmediate(_preview);
            }
            _preview = null;
            _previewRenderers = null;
            _active = null;
            _canPlace = false;
            _interactionUi?.Publish(new InteractionUIMessage(false, string.Empty, string.Empty));
        }

        private void Update()
        {
            if (!IsPlacing) return;
            UpdateCandidate();
            if (Time.frameCount == _beganFrame) return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard?.escapeKey.wasPressedThisFrame == true || mouse?.rightButton.wasPressedThisFrame == true)
            {
                Cancel();
                return;
            }

            if (keyboard?.rKey.wasPressedThisFrame == true)
            {
                _rotation = Quaternion.AngleAxis(_active.rotationStep, Vector3.up) * _rotation;
                UpdateCandidate();
            }

            if (mouse?.leftButton.wasPressedThisFrame == true) TryConfirm();
        }

        private void UpdateCandidate()
        {
            Transform cameraTransform = view ? view : Camera.main ? Camera.main.transform : null;
            if (!cameraTransform || _active == null || !_preview) return;

            Ray ray = new(cameraTransform.position, cameraTransform.forward);
            bool hitSurface = Physics.Raycast(ray, out RaycastHit hit, _active.maxDistance,
                placementSurfaceMask, QueryTriggerInteraction.Ignore);
            if (hitSurface)
            {
                _candidatePosition = Snap(hit.point + hit.normal * _active.surfaceOffset, _active.gridSize);
                _canPlace = Vector3.Dot(hit.normal, Vector3.up) >= _active.minimumSurfaceUp &&
                            !OverlapsBlockingCollider(hit.collider);
            }
            else
            {
                _candidatePosition = ray.GetPoint(_active.maxDistance);
                _canPlace = false;
            }

            _preview.transform.SetPositionAndRotation(_candidatePosition, _rotation);
            ApplyPreviewColor(_canPlace ? validColor : invalidColor);
            if (_hasPublishedState && _publishedCanPlace == _canPlace) return;
            _publishedCanPlace = _canPlace;
            _hasPublishedState = true;
            _interactionUi?.Publish(new InteractionUIMessage(
                true, _canPlace ? _validMessage : _invalidMessage, "LMB · R · RMB"));
        }

        private bool OverlapsBlockingCollider(Collider surface)
        {
            Vector3 center = _candidatePosition + _rotation * _active.boundsCenter + Vector3.up * .015f;
            Vector3 halfExtents = Vector3.Max(_active.boundsSize * .49f, Vector3.one * .01f);
            int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapBuffer, _rotation, blockingMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = _overlapBuffer[i];
                if (!candidate || candidate == surface || candidate.transform.IsChildOf(_preview.transform)) continue;
                return true;
            }
            return false;
        }

        private void TryConfirm()
        {
            if (!_canPlace)
            {
                _notifications?.Show("건축 불가", "바닥 경사나 주변 충돌을 확인하세요.", "!",
                    NotificationKind.Warning, 2.5f);
                return;
            }

            if (!HasRequiredMaterials(_active.blueprintId, out string failure))
            {
                _notifications?.Show("재료 부족", failure, "!", NotificationKind.Warning, 3f);
                Cancel();
                return;
            }

            GameObject placed = Instantiate(_active.structurePrefab, _candidatePosition, _rotation);
            placed.name = _active.DisplayName;
            foreach (var placeable in placed.GetComponentsInChildren<MonoBehaviour>(true))
                if (placeable is IPlaceable feature) feature.Place();
            ConsumeMaterials(_active.blueprintId);
            _notifications?.Show("건축 완료", _active.DisplayName, "◆", NotificationKind.Info, 3f);
            Cancel();
        }

        private bool HasRequiredMaterials(int blueprintId, out string failure)
        {
            failure = string.Empty;
            var blueprint = _blueprints?.GetBluePrint(blueprintId);
            if (blueprint == null)
            {
                failure = "청사진 데이터를 찾을 수 없습니다.";
                return false;
            }

            Dictionary<int, int> totals = GroupRecipe(blueprint.recipe);
            foreach (var pair in totals)
            {
                if (_inventory != null && _inventory.HasItem(pair.Key, pair.Value)) continue;
                failure = $"필요 재료가 부족합니다. (아이템 {pair.Key} × {pair.Value})";
                return false;
            }
            return true;
        }

        private void ConsumeMaterials(int blueprintId)
        {
            var blueprint = _blueprints?.GetBluePrint(blueprintId);
            if (blueprint == null || _inventory == null) return;
            foreach (var pair in GroupRecipe(blueprint.recipe))
                _inventory.RemoveItem(pair.Key, pair.Value);
        }

        private static Dictionary<int, int> GroupRecipe(List<RecipeEntry> recipe)
        {
            var totals = new Dictionary<int, int>();
            if (recipe == null) return totals;
            foreach (RecipeEntry entry in recipe)
            {
                if (entry == null || entry.count <= 0) continue;
                totals.TryGetValue(entry.item, out int current);
                totals[entry.item] = current + entry.count;
            }
            return totals;
        }

        private void ApplyPreviewColor(Color color)
        {
            _properties ??= new MaterialPropertyBlock();
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_Color", color);
            _properties.SetColor("_EmissionColor", color * emissionStrength);
            if (_previewRenderers == null) return;
            foreach (Renderer target in _previewRenderers)
                if (target) target.SetPropertyBlock(_properties);
        }

        private static void DisablePreviewPhysics(GameObject preview)
        {
            foreach (Collider target in preview.GetComponentsInChildren<Collider>(true)) target.enabled = false;
            foreach (Rigidbody body in preview.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private static Vector3 Snap(Vector3 value, float grid)
        {
            if (grid <= .001f) return value;
            value.x = Mathf.Round(value.x / grid) * grid;
            value.z = Mathf.Round(value.z / grid) * grid;
            return value;
        }

        private void OnDestroy() => Cancel();
    }
}
