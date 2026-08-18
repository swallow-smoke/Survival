using _001_Scripts.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public sealed class CreaturePlayerFocusBridge : ITickable
    {
        private readonly IPlayerTransformProvider player;
        private readonly IWorldCreatureGateway creatures;
        private bool wasValid;

        [Inject]
        public CreaturePlayerFocusBridge(IPlayerTransformProvider player, IWorldCreatureGateway creatures)
        {
            this.player = player;
            this.creatures = creatures;
        }

        public void Tick()
        {
            if (!creatures.IsReady) return;

            Transform target = player?.PlayerTrs;
            if (target == null)
            {
                if (!wasValid) return;
                creatures.ClearPlayerFocus();
                wasValid = false;
                return;
            }

            creatures.SetPlayerFocus(target.position);
            wasValid = true;
        }
    }
}
