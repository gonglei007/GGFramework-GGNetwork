using SimpleJson;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 消息（Pomelo 应用层消息：请求/响应/推送）。
    /// </summary>
    public class Message
    {
        public MessageType type;
        public string route;
        public uint id;
        public JsonObject data;

        public Message(MessageType type, uint id, string route, JsonObject data)
        {
            this.type = type;
            this.id = id;
            this.route = route;
            this.data = data;
        }
    }
}
