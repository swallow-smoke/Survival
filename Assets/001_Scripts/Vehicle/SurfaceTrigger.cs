using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Structure
{
    [RequireComponent(typeof(Collider))]
    public class SurfaceTrigger : MonoBehaviour
    {
        [SerializeField] private LayerMask surfaceLayer;

        private ISurfaceDetectable _target;

        private void Awake()
        {
            _target = GetComponentInParent<ISurfaceDetectable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((surfaceLayer.value & (1 << other.gameObject.layer)) != 0)
                _target?.OnReachedSurface();
        }
    }
}
