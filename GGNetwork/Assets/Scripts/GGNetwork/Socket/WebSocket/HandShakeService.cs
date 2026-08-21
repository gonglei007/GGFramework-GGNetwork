using System;
using System.Text;
using SimpleJson;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 握手服务（Pomelo 协议握手）。
    ///
    /// 流程：
    ///   1. 构建握手消息（sys:version/type + user），发送 PKG_HANDSHAKE。
    ///   2. 收到服务端握手响应后校验 code==200 和 sys 字段。
    ///   3. 解析 sys 中的 dict（路由字典）、heartbeat 间隔。
    ///   4. 发送 PKG_HANDSHAKE_ACK 确认，然后调用业务回调。
    /// </summary>
    public class HandShakeService
    {
        public const string Version = "0.3.0";
        public const string Type = "unity-websocket";

        private readonly IProtocolSession session;
        private Action<JsonObject> callback;
        private MessageProtocol messageProtocol;
        private HeartBeatService heartBeatService;

        /// <summary>当前握手下发的路由字典（供消息编解码使用）。</summary>
        public MessageProtocol MessageProtocol { get { return messageProtocol; } }
        public HeartBeatService HeartBeatService { get { return heartBeatService; } }

        public HandShakeService(IProtocolSession session)
        {
            this.session = session;
        }

        public void Request(JsonObject user, Action<JsonObject> callback)
        {
            byte[] body = Encoding.UTF8.GetBytes(BuildMsg(user).ToString());
            session.Send(PackageType.PKG_HANDSHAKE, body);
            this.callback = callback;
        }

        /// <summary>处理服务端握手响应数据。</summary>
        public bool ProcessHandshakeData(JsonObject msg, out string error)
        {
            error = null;
            this.messageProtocol = null;
            if (this.heartBeatService != null) { this.heartBeatService.Stop(); this.heartBeatService = null; }

            // 校验
            if (!msg.ContainsKey("code") || !msg.ContainsKey("sys")
                || Convert.ToInt32(msg["code"]) != 200)
            {
                error = "Handshake error! Please check your handshake config.";
                return false;
            }

            JsonObject sys = (JsonObject)msg["sys"];

            // 路由字典（可选）
            JsonObject dict = new JsonObject();
            if (sys.ContainsKey("dict")) dict = (JsonObject)sys["dict"];
            this.messageProtocol = new MessageProtocol(dict);

            // 心跳间隔（毫秒）
            int interval = 0;
            if (sys.ContainsKey("heartbeat")) interval = Convert.ToInt32(sys["heartbeat"]);
            if (interval > 0)
            {
                this.heartBeatService = new HeartBeatService(interval, this.session);
                this.heartBeatService.Start();
            }

            // 发送握手 ack
            this.session.Send(PackageType.PKG_HANDSHAKE_ACK);

            // 业务回调
            JsonObject userObj = new JsonObject();
            if (msg.ContainsKey("user")) userObj = (JsonObject)msg["user"];
            if (callback != null)
            {
                try { callback.Invoke(userObj); }
                catch (Exception e) { UnityEngine.Debug.LogError(e); }
            }
            return true;
        }

        public void StopHeartBeat()
        {
            if (heartBeatService != null) heartBeatService.Stop();
        }

        private static JsonObject BuildMsg(JsonObject user)
        {
            if (user == null) user = new JsonObject();
            JsonObject msg = new JsonObject();
            JsonObject sys = new JsonObject();
            sys["version"] = Version;
            sys["type"] = Type;
            msg["sys"] = sys;
            msg["user"] = user;
            return msg;
        }
    }
}
