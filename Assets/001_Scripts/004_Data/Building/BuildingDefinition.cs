using System;
using UnityEngine;

namespace _001_Scripts.Data.Building
{
    [Serializable]
    public sealed class BuildingDefinition
    {
        [Min(0)] public int blueprintId;
        public string displayName;
        [Tooltip("Scene-editable prefab created when placement is confirmed.")]
        public GameObject structurePrefab;
        [Tooltip("Dedicated scene-editable hologram prefab used while aiming.")]
        public GameObject previewPrefab;

        [Header("Placement")]
        [Min(.5f)] public float maxDistance = 8f;
        [Min(0f)] public float gridSize = .25f;
        [Range(1f, 180f)] public float rotationStep = 15f;
        [Range(-1f, 1f)] public float minimumSurfaceUp = .55f;
        [Min(0f)] public float surfaceOffset = .025f;

        [Header("Collision Bounds")]
        public Vector3 boundsCenter = new(0f, .15f, 0f);
        public Vector3 boundsSize = new(3.8f, .3f, 3.8f);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? structurePrefab ? structurePrefab.name : $"Building {blueprintId}"
            : displayName;
    }
}
