using System;
using _001_Scripts.Data.Item;
using Unity.Entities;
using UnityEngine;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts._000_Core._000_World._002_Entity.Interface
{
    public interface IWorldResourceGateway
    {
        bool TryRaycast(Vector3 origin, Vector3 direction, float distance, out Entity target, out float fraction);
        bool TryGetInteractionInfo(Entity target, out ResourceInteractionInfo info);
        bool TryHarvest(Entity target, HarvestToolSelection selection);
        bool TryPickup(Entity target);
        int ProcessInventoryTransfers(Func<int, int, int> acceptItems);
    }
}
