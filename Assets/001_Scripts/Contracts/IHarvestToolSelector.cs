using AstraNope.Data.Items;
using WorldBuilder.Entities.Resources;

namespace AstraNope.Contracts
{
    public interface IHarvestToolSelector
    {
        bool TrySelect(ResourceInteractionInfo resource, out HarvestToolSelection selection);
    }
}
