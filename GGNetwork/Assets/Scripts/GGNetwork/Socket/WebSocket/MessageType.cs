namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 消息类型（与 Pomelo 消息定义一致）。
    /// </summary>
    public enum MessageType
    {
        MSG_REQUEST = 0,
        MSG_NOTIFY = 1,
        MSG_RESPONSE = 2,
        MSG_PUSH = 3,
    }
}
