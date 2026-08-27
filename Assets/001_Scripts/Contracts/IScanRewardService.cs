using AstraNope.WorldObjects.Items;

namespace AstraNope.Contracts
{
    public interface IScanRewardService
    {
        void Grant(ScannableTarget target);
    }
}
