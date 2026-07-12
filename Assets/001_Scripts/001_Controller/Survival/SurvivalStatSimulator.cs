using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using MessagePipe;

namespace _001_Scripts.Controller.Survival
{
    public class SurvivalStatSimulator
    {
        private readonly PlayerStat _stat;
        private readonly IPublisher<PlayerStatMessage> _publisher;
        private float _lastRun;

        public SurvivalStatSimulator(PlayerStat stat, IPublisher<PlayerStatMessage> publisher)
        {
            _stat = stat;
            _publisher = publisher;
        }

        public bool Tick(bool running, float deltaTime, float time)
        {
            bool staminaDepleted = false;

            if (running)
            {
                _stat.ModifyStamina(-_stat.GetStaminaUsage() * deltaTime);
                _stat.ModifyHungry(-_stat.GetHungryUsage() * deltaTime);
                _stat.ModifyWater(-_stat.GetWaterUsage() * deltaTime);

                if (_stat.GetStamina() <= 0)
                    staminaDepleted = true;

                _lastRun = time;
            }
            else if (time - _lastRun >= 1f)
            {
                _stat.ModifyStamina(deltaTime * _stat.GetStaminaCure());
            }

            Publish();
            return staminaDepleted;
        }

        private void Publish()
        {
            _publisher.Publish(new PlayerStatMessage(
                _stat.GetHP(),
                _stat.GetStamina(),
                _stat.GetHungry(),
                _stat.GetWater(),
                _stat.GetOxygen(),
                _stat.GetTemp()));
        }
    }
}
