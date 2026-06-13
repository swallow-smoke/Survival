namespace _001_Scripts.Data.Message
{
    public readonly struct InteractionUIMessage
    {
        public readonly bool isVisible;
        public readonly string label;
        public readonly string promptKey;

        public InteractionUIMessage(bool isVisible, string label, string promptKey = "F")
        {
            this.isVisible = isVisible;
            this.label = label;
            this.promptKey = promptKey;
        }
    }
}
