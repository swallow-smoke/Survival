namespace AstraNope.Data.Messages
{
    public readonly struct UIReqMessage
    {
        public readonly UIReqMsgType msgType;
        public readonly string uiName;
        
        public UIReqMessage(UIReqMsgType type, string name)
        {
            msgType = type;
            uiName = name;
        }
    }

    public enum UIReqMsgType
    {
        Open,
        Close,
        Update,
        Action
    }
}