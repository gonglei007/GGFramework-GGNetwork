using UnityEngine;
using SimpleJson;
using GGFramework.GGNetwork.Socket;
using GGFramework.GGTask;
using System;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// 基于 WebSocket + 内部 Pomelo 协议实现的长连接实例。
    ///
    /// 作为 BaseNetworkClient 的一个插拔实现，替代外部的 PomeloNetworkClient(TCP)。
    /// 复用基类的：请求队列、超时重试、断线自动重连、主线程事件调度、UI/日志解耦。
    /// 通过 WebSocketPomeloClient 承载 WebSocket 传输与 Pomelo 应用层协议。
    /// </summary>
    public class WebSocketNetworkClient : BaseNetworkClient
    {
        protected WebSocketPomeloClient client;
        private Action<JsonObject> onHandShaked;

        public WebSocketNetworkClient(string name) : base(name)
        {
            onHandShaked = (JsonObject rep) =>
            {
                if (onConnected != null) onConnected(rep);
            };

            client = new WebSocketPomeloClient();
            client.OnNetWorkStateChanged += (WsNetWorkState state) =>
            {
                // 事件可能在 .NET 线程池触发，转主线程队列。
                JsonObject param = new JsonObject();
                param["state"] = (int)state;
                InnerEventTrigger(new NetworkEvent(NetWorkStateChangedHandler, param));
            };
        }

        protected override bool IsClientConnected()
        {
            return client != null && client.NetworkState == WsNetWorkState.CONNECTED;
        }

        public override void Disconnect()
        {
            base.Disconnect();
            if (client != null && IsClientConnected())
            {
                client.Disconnect(WsNetWorkState.DISCONNECTED, false);
            }
        }

        /// <summary>订阅服务端推送事件。</summary>
        public void On(string name, Action<JsonObject> action)
        {
            if (client == null) return;
            client.On(name, (JsonObject response) =>
            {
                InnerEventTrigger(new NetworkEvent((object obj) =>
                {
                    action(obj as JsonObject);
                }, response));
            });
        }

        protected override void initClient()
        {
            uiAdaptor.ShowWaiting(true);
            // 构造 ws:// 地址，兼容 host 已是 ws:// 前缀的情况。
            string url = BuildWsUrl(host, port);
            TaskSystem.Instance.QueueJob(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(host) || port <= 0)
                    {
                        InnerEventTrigger(new NetworkEvent((object o) =>
                            OnConnectExceptionHandler("invalid_ws_url"), null));
                        return null;
                    }
                    Debug.LogFormat("[thread-{0}]开始WebSocket连接:{1}", Environment.CurrentManagedThreadId, url);
                    client.Connect(url, null, (JsonObject hs) =>
                    {
                        InnerEventTrigger(new NetworkEvent((object hso) =>
                        {
                            if (onHandShaked != null) onHandShaked(hso as JsonObject);
                        }, hs));
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError("[WS] initClient fail: " + e.Message);
                }
                return null;
            });
        }

        protected override bool InnerConnect()
        {
            // WebSocket 握手已在 Connect 中完成，这里仅确认连接已建立。
            return IsClientConnected();
        }

        protected override void InnerRequest(string route, JsonObject msg, Action<JsonObject> callback)
        {
            if (client != null) client.Request(route, msg, callback);
        }

        private static string BuildWsUrl(string host, int port)
        {
            if (host.StartsWith("ws://") || host.StartsWith("wss://")) return host;
            return string.Format("ws://{0}:{1}", host, port);
        }
    }
}
