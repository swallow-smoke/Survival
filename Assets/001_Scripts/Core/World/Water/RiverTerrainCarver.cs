using UnityEngine;

namespace AstraNope.Core.World.Water
{
    [AddComponentMenu("Survival/Water/River Terrain Carver")]
    public sealed class RiverTerrainCarver : MonoBehaviour
    {
        [SerializeField] private SplineRiverWaterBody river;
        [SerializeField] private Terrain terrain;
        [Min(0f), SerializeField] private float bankFalloff = 3f;

        public SplineRiverWaterBody River => river;
        public Terrain Terrain => terrain;
        public float BankFalloff => bankFalloff;

#if UNITY_EDITOR
        private void Reset()
        {
            river = GetComponent<SplineRiverWaterBody>();
        }
#endif
    }
}
