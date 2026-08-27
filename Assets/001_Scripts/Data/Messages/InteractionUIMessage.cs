namespace AstraNope.Data.Messages
{
    public readonly struct InteractionUIMessage
    {
        public readonly bool isVisible;
        public readonly string label;
        public readonly string promptKey;
        public readonly float progress;
        public readonly bool isWarning;

        public InteractionUIMessage(bool isVisible, string label, string promptKey = "LMB",
            float progress = -1f, bool isWarning = false)
        {
            this.isVisible = isVisible;
            this.label = label;
            this.promptKey = promptKey;
            this.progress = progress;
            this.isWarning = isWarning;
        }
    }
}
