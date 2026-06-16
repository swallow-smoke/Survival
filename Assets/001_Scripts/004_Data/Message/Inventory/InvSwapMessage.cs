namespace _001_Scripts.Data.Message
{
    public readonly struct InvSwapMessage
    {
        public readonly int fromIndex;
        public readonly int toIndex;

        public InvSwapMessage(int from, int to)
        {
            fromIndex = from;
            toIndex = to;
        }
    }
}