namespace _001_Scripts.Data
{
    public readonly struct BlueprintUnlockStatus
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string CategoryPath;
        public readonly string IconResource;
        public readonly bool IsUnlocked;
        public readonly int Progress;
        public readonly int Required;

        public BlueprintUnlockStatus(int id, string name, string categoryPath, string iconResource, bool isUnlocked,
            int progress, int required)
        {
            Id = id;
            Name = name ?? string.Empty;
            CategoryPath = categoryPath ?? "Misc";
            IconResource = iconResource ?? string.Empty;
            IsUnlocked = isUnlocked;
            Required = System.Math.Max(1, required);
            Progress = System.Math.Clamp(progress, 0, Required);
        }
    }
}
