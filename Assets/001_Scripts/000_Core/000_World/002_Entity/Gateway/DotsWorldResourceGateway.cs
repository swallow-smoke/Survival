using System;
using _001_Scripts._000_Core._000_World._002_Entity.Interface;
using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts._000_Core._000_World._002_Entity.Gateway
{
    public sealed class DotsWorldResourceGateway : IWorldResourceGateway
    {
        public bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity target,
            out float fraction)
        {
            return WorldResourceCommandQueue.TryRaycast(origin, direction, distance, out target, out fraction);
        }

        public bool TryGetInteractionInfo(Entity target, out ResourceInteractionInfo info)
        {
            return WorldResourceCommandQueue.TryGetInteractionInfo(target, out info);
        }

        public bool TryHarvest(Entity target, HarvestToolSelection selection)
        {
            return WorldResourceCommandQueue.TryHarvest(target, selection.Method, selection.ItemId,
                selection.Tier, selection.Power, selection.Damage, out _);
        }

        public bool TryPickup(Entity target)
        {
            return WorldResourceCommandQueue.TryPickup(target, out _);
        }

        public int ProcessInventoryTransfers(Func<int, int, int> acceptItems)
        {
            return WorldResourceCommandQueue.ProcessInventoryTransfers(acceptItems);
        }
    }
}
