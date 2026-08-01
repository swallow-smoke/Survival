using _001_Scripts.Interface;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Managers
{
    public sealed class DotsResourceInventoryBridge : ITickable
    {
        private readonly IInventoryService inventory;
        private readonly IWorldResourceGateway resourceGateway;

        [Inject]
        public DotsResourceInventoryBridge(IInventoryService inventory, IWorldResourceGateway resourceGateway)
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
