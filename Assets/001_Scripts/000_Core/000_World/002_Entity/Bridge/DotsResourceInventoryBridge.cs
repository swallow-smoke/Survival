using _001_Scripts._000_Core._000_World._002_Entity.Interface;
using _001_Scripts.Interface;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts._000_Core._000_World._002_Entity.Bridge
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
