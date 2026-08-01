using System;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts.Data.Item
{
    [Serializable]
    public struct HarvestToolDefinition
    {
        public int itemId;
        public HarvestMethod method;
        [Min(0)] public int tier;
        [Min(0f)] public float power;
        [Min(0f)] public float damage;
    }

    public readonly struct HarvestToolSelection
    {
        public readonly HarvestMethod Method;
        public readonly int ItemId;
        public readonly byte Tier;
        public readonly float Power;
        public readonly float Damage;

        public HarvestToolSelection(HarvestMethod method, int itemId, byte tier, float power, float damage)
        {
            Method = method;
            ItemId = itemId;
            Tier = tier;
            Power = power;
            Damage = damage;
        }
    }

    [CreateAssetMenu(fileName = "HarvestToolCatalog", menuName = "Data/Harvest Tool Catalog")]
    public sealed class HarvestToolCatalog : ScriptableObject
    {
        [Header("Hand")]
        [Min(0f), SerializeField] private float handPower = 1f;
        [Min(0f), SerializeField] private float handDamage = 10f;
        [SerializeField] private HarvestToolDefinition[] tools = Array.Empty<HarvestToolDefinition>();

        public float HandPower => Mathf.Max(0f, handPower);
        public float HandDamage => Mathf.Max(0f, handDamage);
        public int ToolCount => tools?.Length ?? 0;

        public HarvestToolDefinition GetTool(int index)
        {
            if (tools == null) throw new ArgumentOutOfRangeException(nameof(index));
            return tools[index];
        }
    }
}
