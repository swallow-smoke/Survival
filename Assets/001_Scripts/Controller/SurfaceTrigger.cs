using _001_Scripts.Interface;
using UnityEngine;

namespace _001_Scripts.Controller
{
    /// <summary>
    /// 임시 수면 감지용 트리거. WaterManager 연동 전까지 사용.
    /// 수면 근처에 평면 콜라이더(IsTrigger)로 배치.
    /// </summary>
    public class SurfaceTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var detectable = other.GetComponentInParent<ISurfaceDetectable>();
            detectable?.OnReachedSurface();
        }
    }
}