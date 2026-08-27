using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Contracts;
using VContainer;
using VContainer.Unity;

namespace AstraNope.Core.World.Entities.Bridges
{
    public sealed class DotsResourceInventoryBridge : ITickable
    {
        private readonly IInventoryWriter inventory;
        private readonly IWorldResourceGateway resourceGateway;

        [Inject]
        public DotsResourceInventoryBridge(IInventoryWriter inventory, IWorldResourceGateway resourceGateway)
        {
            this.inventory = inventory;
            this.resourceGateway = resourceGateway;
        }

        public void Tick()
        {
            resourceGateway.ProcessInventoryTransfers((itemId, requested) =>
            {
                var result = inventory.AddItem(itemId, requested);
                return requested - result.remain;
            });
        }
    }
}
