using UnityEngine;
using System.Collections;
using SimpleJson;
using Pomelo.DotNetClient;
using System;
using System.Threading;
using GGFramework.GGTask;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// 基于PomeloClient实现的长连接实例。
    /// 处理连接。
    /// 处理请求。
    /// </summary>
    public class PomeloNetworkClient : IClient
    {
        /// <summary>
        /// 用户未注册
        /// </summary>
        public PomeloClient client = new PomeloClient(IClient.ConnectTimeout * 1000);

        private Action<JsonObject> onHandShaked = null;

        public PomeloNetworkClient(string name): base(name)
        {
            onHandShaked += (JsonObject rep) =>
            {
                if (onConnected != null)
                {
                    onConnected(rep);
                }
            };
            client.NetWorkStateChangedEvent += (Pomelo.DotNetClient.NetWorkState state) =>
            {
                Debug.LogWarning("状态变化-发生事件:" + state.ToString());
                // 因为这个事件可能会在子线程触发，所以先转到消息队列中。
                JsonObject param = new JsonObject();
                param["state"] = (int)state;
                InnerEventTrigger(new NetworkEvent(NetWorkStateChangedHandler, param));
            };
        }

        protected bool IsClientConnected() {
            return client.NetworkState is not ((Pomelo.DotNetClient.NetWorkState)NetWorkState.CONNECTED or (Pomelo.DotNetClient.NetWorkState)NetWorkState.CONNECTING);
        }

        public virtual void Disconnect()
        {
            base.Disconnect();
            if (IsClientConnected())
            {
                client.disconnect();
            }
        }


        public void On(string name, Action<JsonObject> action)
        {
            client.on(name, (JsonObject response) =>
            {
                InnerEventTrigger(new NetworkEvent((object obj) => {
                    action(obj as JsonObject);
                }, response));
            });
        }

        private void initClient()
        {
            base.initClient();
            //Debug.LogFormat("[thread-{0}]准备启动连接任务！", Thread.CurrentThread.ManagedThreadId);
            TaskSystem.Instance.QueueJob(() =>
            {
                Debug.LogFormat("[thread-{0}]开始连接:{1}:{2}", Thread.CurrentThread.ManagedThreadId, host, port);
                client.initClient(host, port);
                return null;
            });
        }
        private bool InnerConnect()
        {
            bool connected = base.InnerConnect();
            Debug.Log("开始握手");
            connected = client.connect(null, (JsonObject handshakeResult) =>
            {
                InnerEventTrigger(new NetworkEvent(
                    (object handshakeObj) =>
                    {
                        JsonObject handshakeParam = handshakeObj as JsonObject;
                        this.onHandShaked(handshakeParam);
                    },
                    handshakeResult)
                );
            });
            return connected;
        }

        private void InnerRequest(string route, JsonObject msg, Action<JsonObject> callback)
        {
            base.InnerRequest(route, msg, (JsonObject response)=> {
                client.request(route, msg, callback);
            });
        }
    }
}

