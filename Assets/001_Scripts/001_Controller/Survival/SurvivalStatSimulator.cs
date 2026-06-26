using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using MessagePipe;

namespace _001_Scripts.Controller.Survival
{
    /// <summary>
    /// 생존 스탯 시뮬레이션(달리기 중 자원 소모 / 휴식 시 스태미나 회복)과
    /// 매 프레임 스탯 메시지 발행을 담당한다.
    /// PlayerController가 소유하며, 발행자는 컨테이너에서 받아 생성자로 주입한다.
    /// </summary>
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

        /// <summary>매 프레임 호출. 달리는 중이면 자원을 소모하고, 아니면 일정 시간 뒤 스태미나를 회복한 뒤 스탯을 발행한다.</summary>
        /// <returns>스태미나 고갈로 달리기를 중단해야 하면 true.</returns>
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
