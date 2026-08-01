using _001_Scripts.Data.Item;
using WorldBuilder.Entities.Resources;

namespace _001_Scripts.Interface
{
    public interface IHarvestToolSelector
    {
        bool TrySelect(ResourceInteractionInfo resource, out HarvestToolSelection selection);
    }
}
