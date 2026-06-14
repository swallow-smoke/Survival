using _001_Scripts.Structure;
using UnityEngine;

namespace _001_Scripts.Controller
{
    // 부력 컨트롤러가 자동 생성함. 붙이지 말것
    public class BuoyancyTriggerProxy : MonoBehaviour
    {
        private BuoyancyController _owner;

        public void Initialize(BuoyancyController owner) => _owner = owner;

        private void OnTriggerEnter(Collider other)
        {
            var water = other.GetComponent<WaterVolume>();
            if (water != null) _owner.HandleEnterWater(water);
        }

        private void OnTriggerExit(Collider other)
        {
            var water = other.GetComponent<WaterVolume>();
            if (water != null) _owner.HandleExitWater(water);
        }
    }
}