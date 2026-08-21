using System;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGNetwork.Socket
{
    /// <summary>
    /// 基于 WebSocket 传输 + Pomelo 协议的客户端（内部实现）。
    ///
    /// 等同于外部 PomeloClient 的能力：
    ///   - Connect(host, port, path, user, callback)：握手建立长连接
    ///   - Request(route, msg, callback)：请求/响应
    ///   - Notify(route, msg)：通知（无响应）
    ///   - On(route, action)：订阅服务端推送
    ///   - Disconnect(reason)：主动断开
    ///   - OnStateChanged：连接状态事件
    ///
    /// 协议栈：WebSocketClient（传输）→ PackageStream（切包）→
    ///   Protocol（状态机）→ MessageProtocol（消息编解码）→ EventManager（回调分发）
    ///   HandShakeService / HeartBeatService（握手 / 心跳）。
    ///
    /// 本类在线程安全设计：传输层事件在 .NET 线程池上触发，本类直接透传；
    /// 上层（WebSocketNetworkClient）负责把回调调度回 Unity 主线程。
    /// </summary>
    public class WebSocketPomeloClient : IDisposable, IProtocolSession
    {
        // ---- 状态事件 ----
        public event Action<WsNetWorkState> OnNetWorkStateChanged;

        private readonly object reqIdLock = new object();
        private uint reqId = 1;

        private WebSocketClient ws;
        private PackageStream stream;
        private HandShakeService handshake;
        private MessageProtocol messageProtocol;
        private EventManager eventManager;

        private WsNetWorkState state = WsNetWorkState.CLOSED;
        private readonly object stateLock = new object();

        public WsNetWorkState NetworkState { get { lock (stateLock) return state; } }

        public WebSocketPomeloClient()
        {
            eventManager = new EventManager();
        }

        // ---- 连接 ----

        /// <summary>
        /// 建立连接并握手。
        /// url 形如 ws://host:port[/path]，user 为握手附带用户信息。
        /// </summary>
        public void Connect(string url, JsonObject user, Action<JsonObject> handshakeCallback)
        {
            Disconnect(WsNetWorkState.CLOSED, true);

            ChangeState(WsNetWorkState.CONNECTING);

            ws = new WebSocketClient();
            ws.OnConnected += OnWsConnected;
            ws.OnError += OnWsError;
            ws.OnClosed += OnWsClosed;
            ws.OnDataReceived += OnWsData;

            stream = new PackageStream(OnPackage);
            handshake = new HandShakeService(this);

            ws.Connect(url);
            pendingHandshakeCallback = handshakeCallback;
        }

        private Action<JsonObject> pendingHandshakeCallback;

        private void OnWsConnected()
        {
            // 发起握手
            try
            {
                if (handshake != null) handshake.Request(null, pendingHandshakeCallback);
            }
            catch (Exception e)
            {
                Debug.LogError("[WS] handshake request fail: " + e.Message);
            }
        }

        private void OnWsData(byte[] data)
        {
            if (stream != null) stream.Feed(data);
        }

        private void OnWsError(string msg)
        {
            Debug.LogError("[WS] error: " + msg);
            ChangeState(WsNetWorkState.ERROR);
            DisposeInner();
            ChangeState(WsNetWorkState.DISCONNECTED);
        }

        private void OnWsClosed()
        {
            ChangeState(WsNetWorkState.DISCONNECTED);
            DisposeInner();
        }

        private void OnPackage(Package pkg)
        {
            switch (pkg.type)
            {
                case PackageType.PKG_HANDSHAKE:
                    // 处理服务端握手响应
                    if (handshake != null)
                    {
                        string error;
                        var data = SimpleJson.SimpleJson.DeserializeObject(
                            System.Text.Encoding.UTF8.GetString(pkg.body)) as JsonObject;
                        if (data != null && handshake.ProcessHandshakeData(data, out error))
                        {
                            messageProtocol = handshake.MessageProtocol;
                            ChangeState(WsNetWorkState.CONNECTED);
                        }
                        else
                        {
                            Debug.LogError("[WS] handshake fail: " + (error ?? "invalid data"));
                            ChangeState(WsNetWorkState.ERROR);
                            DisposeInner();
                        }
                    }
                    break;

                case PackageType.PKG_HEARTBEAT:
                    if (handshake != null && handshake.HeartBeatService != null)
                        handshake.HeartBeatService.ResetTimeout();
                    break;

                case PackageType.PKG_DATA:
                    if (handshake != null && handshake.HeartBeatService != null)
                        handshake.HeartBeatService.ResetTimeout();
                    ProcessMessageBody(pkg.body);
                    break;

                case PackageType.PKG_KICK:
                    Debug.LogWarning("[WS] kicked by server.");
                    ChangeState(WsNetWorkState.DISCONNECTED);
                    DisposeInner();
                    break;
            }
        }

        private void ProcessMessageBody(byte[] body)
        {
            if (messageProtocol == null) return;
            Message msg = messageProtocol.Decode(body);
            if (msg == null) return;

            if (msg.type == MessageType.MSG_RESPONSE)
            {
                eventManager.InvokeCallBack(msg.id, msg.data);
            }
            else if (msg.type == MessageType.MSG_PUSH)
            {
                eventManager.InvokeOnEvent(msg.route, msg.data);
            }
        }

        // ---- 请求 / 通知 / 订阅 ----

        public void Request(string route, JsonObject msg, Action<JsonObject> callback)
        {
            if (state != WsNetWorkState.CONNECTED) return;
            if (messageProtocol == null || ws == null || !ws.IsOpen) return;

            uint id;
            lock (reqIdLock)
            {
                id = reqId++;
            }
            eventManager.AddCallBack(id, callback);

            byte[] data;
            try { data = messageProtocol.Encode(route, id, msg ?? new JsonObject()); }
            catch (Exception e)
            {
                Debug.LogError("[WS] encode fail: " + e.Message);
                return;
            }
            Send(PackageType.PKG_DATA, data);
        }

        public void Notify(string route, JsonObject msg)
        {
            if (state != WsNetWorkState.CONNECTED) return;
            if (messageProtocol == null || ws == null || !ws.IsOpen) return;

            byte[] data;
            try { data = messageProtocol.Encode(route, 0, msg ?? new JsonObject()); }
            catch (Exception e)
            {
                Debug.LogError("[WS] encode fail: " + e.Message);
                return;
            }
            Send(PackageType.PKG_DATA, data);
        }

        public void On(string route, Action<JsonObject> action)
        {
            eventManager.AddOnEvent(route, action);
        }

        public void Disconnect(WsNetWorkState finalState, bool reset)
        {
            if (!reset && ws == null && stream == null) return;

            if (handshake != null) handshake.StopHeartBeat();
            DisposeInner();

            ChangeState(finalState);
        }

        // ---- IProtocolSession ----

        public void Send(PackageType type, byte[] body)
        {
            if (ws == null || !ws.IsOpen) return;
            ws.SendBinary(PackageProtocol.Encode(type, body));
        }

        public void Send(PackageType type)
        {
            if (ws == null || !ws.IsOpen) return;
            ws.SendBinary(PackageProtocol.Encode(type));
        }

        public void OnStateChanged(WsNetWorkState s)
        {
            // 心跳超时等内部触发
            ChangeState(s);
        }

        // ---- 内部 ----

        private void ChangeState(WsNetWorkState s)
        {
            bool changed = false;
            lock (stateLock)
            {
                if (state != s)
                {
                    state = s;
                    changed = true;
                }
            }
            if (changed && OnNetWorkStateChanged != null)
            {
                try { OnNetWorkStateChanged(state); }
                catch (Exception e) { Debug.LogError(e); }
            }
        }

        private void DisposeInner()
        {
            if (stream != null) { stream.Dispose(); stream = null; }
            if (ws != null) { ws.OnConnected -= OnWsConnected; ws.OnError -= OnWsError; ws.OnClosed -= OnWsClosed; ws.OnDataReceived -= OnWsData; ws.Dispose(); ws = null; }
            if (handshake != null) { handshake.StopHeartBeat(); handshake = null; }
            messageProtocol = null;
        }

        public void Dispose()
        {
            DisposeInner();
            if (eventManager != null) { eventManager.Dispose(); eventManager = null; }
        }
    }
}
