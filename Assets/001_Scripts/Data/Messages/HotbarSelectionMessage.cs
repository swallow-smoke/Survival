namespace AstraNope.Data.Messages
{
    public readonly struct HotbarSelectionMessage
    {
        public readonly int Index;
        public HotbarSelectionMessage(int index) => Index = index;
    }
}
