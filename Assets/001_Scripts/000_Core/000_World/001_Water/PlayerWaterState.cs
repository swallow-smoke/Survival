using _001_Scripts.Core._000_World._001_Water.Interface;

namespace _001_Scripts.Core._000_World._001_Water
{
    public readonly struct PlayerWaterState
    {
        public readonly bool TouchingWater;
        public readonly bool Wading;
        public readonly bool Swimming;
        public readonly bool HeadUnderwater;
        public readonly bool CameraUnderwater;
        public readonly IWaterBody ActiveWaterBody;
        public readonly WaterSample ChestSample;
        public readonly WaterSample CameraSample;

        public PlayerWaterState(bool touchingWater, bool wading, bool swimming,
            bool headUnderwater, bool cameraUnderwater, IWaterBody activeWaterBody,
            WaterSample chestSample, WaterSample cameraSample)
        {
            TouchingWater = touchingWater;
            Wading = wading;
            Swimming = swimming;
            HeadUnderwater = headUnderwater;
            CameraUnderwater = cameraUnderwater;
            ActiveWaterBody = activeWaterBody;
            ChestSample = chestSample;
            CameraSample = cameraSample;
        }

        public bool Equals(PlayerWaterState other) =>
            TouchingWater == other.TouchingWater && Wading == other.Wading &&
            Swimming == other.Swimming && HeadUnderwater == other.HeadUnderwater &&
            CameraUnderwater == other.CameraUnderwater && ReferenceEquals(ActiveWaterBody, other.ActiveWaterBody);
    }
}
