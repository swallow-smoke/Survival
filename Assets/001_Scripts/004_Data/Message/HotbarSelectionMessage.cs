namespace _001_Scripts.Data.Message
{
    public readonly struct HotbarSelectionMessage
    {
        public readonly int Index;
        public HotbarSelectionMessage(int index) => Index = index;
    }
}
