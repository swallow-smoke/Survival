using System.Collections.Generic;
using System.Linq.Expressions;
using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;

namespace _001_Scripts.Core._000_World._001_Water
{
    public class WaterQueryService : MonoBehaviour, IWaterQuery, IWaterRegistry
    {
        private List<IWaterbody> _bodies = new();
        [SerializeField] private OceanBody _oceanBody;

        public void Register(IWaterbody waterbody) => _bodies.Add(waterbody);
        public void UnRegister(IWaterbody waterbody) => _bodies.Remove(waterbody);

        public bool IsInWater(Vector3 position)
        {
            foreach (var wb in _bodies)
            {
                if (wb.Contain(position))
                    return true;
            }

            return false;
        }

        public float GetSurfaceY(Vector3 position) =>
            GetWaterBody(position).GetSurfaceY(position);

        public IWaterbody GetWaterBody(Vector3 position)
        {
            foreach (var wb in _bodies)
            {
                if (wb.Contain(position)) return wb;
            }
            
            return _oceanBody;
        }
    }
}