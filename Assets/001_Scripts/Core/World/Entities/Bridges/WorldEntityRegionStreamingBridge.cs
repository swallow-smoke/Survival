using System.Collections.Generic;
using AstraNope.Contracts;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WorldBuilder.Entities;
using WorldBuilder.Runtime.Grid;

namespace AstraNope.Core.World.Entities.Bridges
{
    public sealed class WorldEntityRegionStreamingBridge : ITickable
    {
        private const int RegionRadius = 1;

        private readonly IPlayerTransformProvider player;
        private readonly WorldGridSettings gridSettings;
        private readonly List<RegionCoord> loadedRegions = new List<RegionCoord>(9);
        private RegionCoord previousRegion;
        private bool initialized;

        [Inject]
        public WorldEntityRegionStreamingBridge(IPlayerTransformProvider player, WorldGridSettings gridSettings)
        {
            this.player = player;
            this.gridSettings = gridSettings;
        }

        public void Tick()
        {
            if (gridSettings == null || !WorldEntityCommandQueue.IsReady) return;
            Transform target = player?.PlayerTrs;
            if (target == null) return;

            WorldGrid grid = gridSettings.CreateGrid();
            RegionCoord center = grid.WorldToRegion(target.position);
            if (initialized && center == previousRegion) return;

            initialized = true;
            previousRegion = center;
            loadedRegions.Clear();
            for (int x = -RegionRadius; x <= RegionRadius; x++)
            for (int z = -RegionRadius; z <= RegionRadius; z++)
                loadedRegions.Add(new RegionCoord(center.X + x, center.Z + z));
            loadedRegions.Sort();
            WorldEntityCommandQueue.SetLoadedRegions(loadedRegions);
        }
    }
}
