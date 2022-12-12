using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJson;

namespace GGFramework.GGNetwork
{
    /**
     * 基于websocket的网络系统。
     * 特性支持：
     * TODO：GL-归纳下面的特性。
     * TODO: GL - 解除UISystem的强依赖。
     * TODO: GL - 支持消息压缩解包。
     */
    public class NetworkSystem : Singleton<NetworkSystem>
    {
        public struct ServiceInfo
        {
            public ServiceInfo(string host, int port)
            {
                this.host = host;
                this.port = port;
            }
            public string host;
            public int port;
        }
        private Dictionary<string, NetworkClient> clientMap = new Dictionary<string, NetworkClient>();
        private Dictionary<string, ServiceInfo> serviceInfoMap = new Dictionary<string, ServiceInfo>();

        public NetworkSystem()
        {
            CreateNetworkClient("game", true);
            CreateNetworkClient("notify", false);
        }

        public void SetServiceInfo(string service, string url)
        {
            string[] urlParams = url.Split(':');
            if (urlParams.Length < 2)
            {
                string errorMessage = "Illegal URL!!!-" + url;
                throw new Exception(errorMessage);
            }
            string host = urlParams[0];
            int port = Convert.ToInt32(urlParams[1]);
            serviceInfoMap[service] = new ServiceInfo(host, port);
        }

        public ServiceInfo GetServiceInfo(string service)
        {
            if (!serviceInfoMap.ContainsKey(service))
            {
                string errorMessage = "Not found this service!!!-" + service;
                throw new Exception(errorMessage);
            }
            return serviceInfoMap[service];
        }

        /// <summary>
        /// 创建NetworkClient实例。
        /// </summary>
        /// <param name="name">服务名称</param>
        /// <param name="bindUI">是否有UI响应</param>
        /// <returns></returns>
        public NetworkClient CreateNetworkClient(string name, bool bindUI)
        {
            NetworkClient client = new NetworkClient(name);
            //if (bindUI)
            //{
            //    client.onError = (JsonObject param) => {
            //    };
            //    client.onDialog = (string title, string message, bool retry, Action<bool> callback) => {
            //        if (Global.Mode == Global.Modes.Debug)
            //        {
            //            callback(true);
            //            return;
            //        }
            //        if (UISystem.Instance == null)
            //        {
            //            callback(true);
            //            return;
            //        }
            //        UISystem.Instance.PopDialog(title, message, (int)UIDialog.Option.Yes, (UIDialog.Option option) => {
            //            //TODO: GL - 根据异常响应类型，决定做出哪种回调。
            //            if (option == UIDialog.Option.Yes)
            //            {
            //                // 重试
            //                callback(retry);
            //            }
            //            else
            //            {
            //                // 忽略
            //                callback(retry);
            //            }
            //        });
            //    };
            //    client.onWaiting = (bool show) => {
            //        if (UISystem.Instance == null)
            //        {
            //            return;
            //        }
            //        UISystem.Instance.ShowWaiting(show);
            //    };
            //}
            clientMap[name] = client;
            return client;
        }

        public NetworkClient GetNetworkClient(string name)
        {
            if (!clientMap.ContainsKey(name))
            {
                return null;
            }
            return clientMap[name];
        }

        public void CloseAll()
        {
            foreach (var item in clientMap)
            {
                NetworkClient client = item.Value;
                client.Close();
            }
        }

        public void CloseClient(string name)
        {
            if (clientMap.ContainsKey(name))
            {
                NetworkClient client = clientMap[name];
                client.Close();
            }
        }

        public void DisconnectClient(string name)
        {
            if (clientMap.ContainsKey(name))
            {
                NetworkClient client = clientMap[name];
                client.Disconnect();
            }
        }

        public void OnUpdate()
        {
            foreach (var item in clientMap)
            {
                NetworkClient client = item.Value;
                client.Update();
            }
        }

        public NetworkClient ConnectNetworkClient(string name, string host, int port, Action<JsonObject> callback)
        {
            NetworkClient client = GetNetworkClient(name);
            client.client.disposed = false;
            if (client == null)
            {
                return null;
            }
            JsonObject result = new JsonObject();
            result["code"] = NetworkConst.CODE_OK;
            try
            {
                client.onConnected = (JsonObject param) =>
                {
                    GameDebugger.sPushLog("连接服务器成功!" + param.ToString() + "-" + System.Threading.Thread.CurrentThread.ManagedThreadId);
                    callback(result);
                };
                client.Open(host, port);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                result["msg"] = e.ToString();
                result["code"] = NetworkConst.CODE_FAILED;
                if (callback != null)
                {
                    callback(result);
                }
            }
            return client;
        }

        public void SendRequest(string name, string route, JsonObject message, Action<JsonObject> response)
        {
            NetworkClient client = GetNetworkClient(name);
            if (client == null)
            {
                return;
            }
            NetworkRequest request = new NetworkRequest(response, null, route, message);
            client.Request(request);
        }

        public void SendRequestX(string name, string route, string message, Action<JsonObject> response)
        {
            try
            {
                JsonObject messageObject = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(message);
                SendRequest(name, route, messageObject, response);
            }
            catch (Exception e)
            {
                //TODO: GL - deal exception!!!
            }
        }

        public void OnApplicationQuit()
        {
            CloseAll();
        }

        public void Dispose()
        {
            return;
            CloseAll();
        }
    }
}

