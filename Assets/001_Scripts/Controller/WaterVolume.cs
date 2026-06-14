using UnityEngine;

namespace _001_Scripts.Controller
{
    // Water 레이어 오브젝트에 붙이는 용도
    public class WaterVolume : MonoBehaviour
    {
        [Tooltip("수면 기준 Transform. 없으면 Collider bounds 상단 사용")]
        [SerializeField] private Transform surfaceTransform;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider == null)
                Debug.LogError("[WaterVolume] isTrigger Collider가 없습니다.");
        }

        public float GetSurfaceY()
        {
            if (surfaceTransform != null) return surfaceTransform.position.y;
            if (_collider != null) return _collider.bounds.max.y;
            return transform.position.y;
        }
    }
}