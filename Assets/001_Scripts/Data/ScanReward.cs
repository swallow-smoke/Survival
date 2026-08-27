using System;

namespace AstraNope.Data
{
    public enum ScanRewardType
    {
        Log,
        BlueprintProgress,
        BlueprintUnlock
    }

    [Serializable]
    public sealed class ScanReward
    {
        public ScanRewardType type;
        public string logId;
        public int blueprintId;
        public int amount = 1;

        public static ScanReward Log(string id) => new()
        {
            type = ScanRewardType.Log,
            logId = id,
            amount = 1
        };

        public static ScanReward BlueprintProgress(int id, int progress = 1) => new()
        {
            type = ScanRewardType.BlueprintProgress,
            blueprintId = id,
            amount = Math.Max(1, progress)
        };

        public static ScanReward BlueprintUnlock(int id) => new()
        {
            type = ScanRewardType.BlueprintUnlock,
            blueprintId = id,
            amount = 1
        };
    }
}
