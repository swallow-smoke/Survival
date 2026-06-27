using System;
using _001_Scripts.Core._000_World._001_Water.Interface;
using UnityEngine;
using VContainer;

namespace _001_Scripts.Core._000_World._001_Water
{
    [RequireComponent(typeof(BoxCollider))]
    public class LocalWaterVol : MonoBehaviour, IWaterbody
    {
        private BoxCollider boxCol;
        private IWaterRegistry _waterRegistry;

        private void Awake()
        {
            boxCol = GetComponent<BoxCollider>();
        }

        public float GetSurfaceY(Vector3 position) => transform.position.y;
        public bool Contain(Vector3 position) => boxCol.bounds.Contains(position);

        private void Start()
        {
            _waterRegistry.Register(this);
        }

        private void OnDisable()
        {
            _waterRegistry.UnRegister(this);
        }


        [Inject]
        public void Contruct(IWaterRegistry regstry)
        {
            _waterRegistry = regstry;
        }
    }
}