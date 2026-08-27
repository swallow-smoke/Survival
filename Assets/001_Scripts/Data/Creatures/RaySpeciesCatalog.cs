using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraNope.Data.Creatures
{
    [Serializable]
    public sealed class RaySpeciesDefinition
    {
        [SerializeField] private string displayName = "Ray";
        [SerializeField] private int prefabId = 2100;
        [SerializeField] private GameObject model;
        [Min(0.01f), SerializeField] private float modelScale = 1f;
        [Min(0f), SerializeField] private float cruiseSpeed = 2f;
        [Min(0f), SerializeField] private float turnSpeedDegrees = 75f;
        [Min(0f), SerializeField] private float wanderRadius = 24f;
        [Min(0f), SerializeField] private float verticalRadius = 7f;
        [Min(0f), SerializeField] private float fleeDistance = 8f;
        [Range(0f, 45f), SerializeField] private float maximumBankDegrees = 18f;
        [Min(0f), SerializeField] private float bankResponsiveness = 4f;
        [SerializeField] private Vector3 spawnCenter;
        [SerializeField] private Vector3 spawnVolume = new Vector3(48f, 14f, 48f);
        [Min(0), SerializeField] private int maximumAlive = 6;
        [Min(1), SerializeField] private int spawnPerTick = 1;
        [Min(0.01f), SerializeField] private float spawnInterval = 8f;

        public string DisplayName => displayName;
        public int PrefabId => prefabId;
        public GameObject Model => model;
        public float ModelScale => modelScale;
        public float CruiseSpeed => cruiseSpeed;
        public float TurnSpeedDegrees => turnSpeedDegrees;
        public float WanderRadius => wanderRadius;
        public float VerticalRadius => verticalRadius;
        public float FleeDistance => fleeDistance;
        public float MaximumBankDegrees => maximumBankDegrees;
        public float BankResponsiveness => bankResponsiveness;
        public Vector3 SpawnCenter => spawnCenter;
        public Vector3 SpawnVolume => spawnVolume;
        public int MaximumAlive => maximumAlive;
        public int SpawnPerTick => spawnPerTick;
        public float SpawnInterval => spawnInterval;

#if UNITY_EDITOR
        public void Configure(string name, int id, GameObject sourceModel, Vector3 center,
            float speed, float scale)
        {
            displayName = name;
            prefabId = id;
            model = sourceModel;
            spawnCenter = center;
            cruiseSpeed = Mathf.Max(0f, speed);
            modelScale = Mathf.Max(0.01f, scale);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Survival/Creatures/Ray Species Catalog", fileName = "RaySpeciesCatalog")]
    public sealed class RaySpeciesCatalog : ScriptableObject
    {
        [SerializeField] private List<RaySpeciesDefinition> species = new List<RaySpeciesDefinition>();

        public IReadOnlyList<RaySpeciesDefinition> Species => species;

#if UNITY_EDITOR
        public List<RaySpeciesDefinition> MutableSpecies => species;
#endif
    }
}
