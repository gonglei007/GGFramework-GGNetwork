using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SimpleJson;
using UnityEngine;
using GGFramework.GGTask;
using System.Collections;

namespace GGFramework.GGNetwork
{
    public enum NetWorkState
    {
        CLOSED,

        CONNECTING,

        CONNECTED,

        DISCONNECTED,

        TIMEOUT,

        ERROR
    }

    public class IClient
    {
        public static int ConnectTimeout = 8;
        public static int RequestTimeout = 10;
        public static uint AutoReconnectTimes = 3;           // 自动重连次数
                                                             //public static float ReconnectionDelay = 1.0f;
        public static float ReconnectionDelay = 4.0f;

        public string host;
        public int port;
        public bool opening = false;    // 设置一个网络连接是否打开。打开状态时，如果网络断开，要持续尝试连接上此连接。
        public bool background = false; // 设置是否为后台运行的连接。如果是后台运行，则不进行UI反馈。

        // Add Action for checkNetwork
        public Action<JsonObject> onConnected = null;
        public Action<JsonObject> onClose = null;
        public Action<JsonObject> onError = null;
        public Action<JsonObject> onConnectionTimeout = null;

        public Action<NetworkRequest> onRequestError = null;

        public Action<string, string, bool, Action<bool>> onDialog = null;
        public Func<string, string> onGetText = null;
        public Action<bool> onWaiting = null;
        private static int messageID = 0;
        private string name = "none";

        public class NetworkEvent
        {
            Action<object> action;
            JsonObject param;
            public NetworkEvent(Action<object> action, JsonObject param)
            {
                this.action = action;
                this.param = param;
            }
            public void DoAction()
            {
                if (this.action != null)
                {
                    this.action(param);
                }
            }
        }

        private ConcurrentQueue<NetworkEvent> innerEventQueue = new ConcurrentQueue<NetworkEvent>();
        /// <summary>
        /// 向服务器请求的消息队列
        /// </summary>
        private List<NetworkRequest> requestList = new List<NetworkRequest>(20);
        private ConcurrentQueue<NetworkRequest> responseQueue = new ConcurrentQueue<NetworkRequest>();

        private uint reconnectCounter = 0;

        public IClient(string name) {
            this.name = name;
        }


        /// <summary>
        /// 获取（本地化）文本。
        /// 如果没有复制本地化文本回调，直接传key。
        /// </summary>
        /// <param name="text"></param>
        protected string GetText(string text)
        {
            if (onGetText == null)
            {
                return text;
            }
            return onGetText(text);
        }

        protected virtual bool IsClientConnected() { return false; }

        protected virtual void initClient()
        {
            if (onWaiting != null)
            {
                onWaiting(true);
            }
        }

        /// <summary>
        /// 内部连接接口。要在子类实现。
        /// </summary>
        protected virtual bool InnerConnect()
        {
            return false;
        }

        protected virtual void InnerRequest(string route, JsonObject msg, Action<JsonObject> callback) { }

        protected IEnumerator delayConnect(float delay)
        {
            //Debug.Log("延时:"+delay.ToString());
            yield return new WaitForSeconds(delay);
            Debug.LogFormat("...{0}秒|开始第{1}次尝试重连！", delay, reconnectCounter);
            this.initClient();
        }

        /// <summary>
        /// 打开连接状态。如果断开了，自动重连。
        /// </summary>
        public void Open(string host, int port, bool background = false)
        {
            opening = true;
            this.host = host;
            this.port = port;
            this.background = background;
            initClient();
        }

        /// <summary>
        /// 断开连接，并关闭连接状态。
        /// </summary>
        public void Close()
        {
            Disconnect();
            opening = false;
        }

        /// <summary>
        /// 断开连接。
        /// 备注：如果还处于打开状态。则会自动尝试重连。
        /// </summary>
        public virtual void Disconnect() { }

        public void Request(NetworkRequest request)
        {
            if (!opening)
            {
                throw new Exception("NetworkClient is not opened yet!!!-" + this.name);
            }

            request.msgID = ++messageID;
            requestList.Add(request);
        }

        /// <summary>
        /// 执行消息发送。
        /// 子线程内发送。
        /// </summary>
        /// <param name="request"></param>
        public void DoSendRequest(NetworkRequest request)
        {
            Debug.Log("发送Request:" + request.route);
            request.ChangeState(NetworkRequest.RequestStates.Processing);
            if (onWaiting != null)
            {
                onWaiting(true);
            }
            if (!IsClientConnected())
            {
                // 网络还没有连上，先报个错，然后重试。
                request.errorMessage = "Network is not connected yet! Please wait a moment!";
                request.ChangeState(NetworkRequest.RequestStates.Error);
                OnRequestFinish(request);
                return;
            }
            else
            {
                TaskSystem.Instance.QueueJob(() =>
                {
                    try
                    {
                        // 开始处理请求
                        request.timer = 0.0f;
                        InnerRequest(request.route, request.msg, (JsonObject response) =>
                        {
                            request.data = response;
                            // 请求返回结果
                            request.ChangeState(NetworkRequest.RequestStates.Finished);
                            InnerDoResponse(request);
                        });
                    }
                    catch (Exception e)
                    {
                        InnerEventTrigger(new NetworkEvent((object obj) => {
                            // 请求发生异常
                            request.errorMessage = e.ToString();
                            request.ChangeState(NetworkRequest.RequestStates.Error);
                            OnRequestFinish(request);
                        }, null));
                        throw e;
                    }
                    return null;
                });
            }
        }


        /// <summary>
        /// 消息请求完成，返回响应。
        /// </summary>
        /// <param name="request"></param>
        public void OnRequestFinish(NetworkRequest request)
        {
            Debug.LogFormat("Request result!!!-route:{0} | state:{1} | error:{2}", request.route, request.State, request.errorMessage);
            //GameDebugger.Instance.PushLogFormat("Request result!!!-route:{0} | state:{1} | error:{2}", request.route, request.State, request.errorMessage);
            //Debug.Log("Request response->" + request.route);
            if (onWaiting != null)
            {
                onWaiting(false);
            }
            switch (request.State)
            {
                case NetworkRequest.RequestStates.Finished:
                    try
                    {
                        if (request.data.ContainsKey("response"))
                        {
                            string responseData = request.data["response"].ToString();
                            string str = ZipUtil.Unzip(responseData);
                            str = str.Replace("\\", "");
                            request.data = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(str);
                        }
                        if (request.data.ContainsKey("code"))
                        {
                            int code = Convert.ToInt32(request.data["code"]);
                            if (code == NetworkConst.CODE_FAILED)
                            {
                                if (request.data.ContainsKey("msg"))
                                {
                                    onDialog(GetText("server_error"), request.data["msg"].ToString(), true, null);
                                }
                                else
                                {
                                    onDialog(GetText("server_error"), "null", true, null);
                                }
                            }
                            else if (code != NetworkConst.CODE_FA_REPEAT_MSG &&  // 不是重复消息
                                code != NetworkConst.CODE_Silent)           // 不是静默消息
                            {
                                request.DoCallback();
                            }
                        }
                        else
                        {
                            GameDebugger.sPushLog("Invalid response data!!!" + request.data.ToString());
                            onDialog(GetText("server_error"), "server_error", true, null);
                        }
                    }
                    catch (Exception e)
                    {
                        request.errorMessage = e.ToString();
                        OnRequestExceptionHandler(request);
                        throw e;
                    }
                    break;
                case NetworkRequest.RequestStates.Error:
                    request.errorMessage = "Request Error!!!";
                    OnRequestExceptionHandler(request);
                    break;
                case NetworkRequest.RequestStates.TimedOut:
                    request.errorMessage = "Request Timeout!!!";
                    OnRequestExceptionHandler(request);
                    break;
                default:
                    break;
            }

        }

        public void Update()
        {
            while (innerEventQueue.Count > 0)
            {
                NetworkEvent oneEvent;
                innerEventQueue.TryDequeue(out oneEvent);
                TriggerEvent(oneEvent);
            }

            while (responseQueue.Count > 0)
            {
                NetworkRequest response;
                if (responseQueue.TryDequeue(out response))
                {
                    OnRequestFinish(response);
                }
            }
            float deltaTime = Time.deltaTime;

            for (int i = requestList.Count - 1; i >= 0; i--)
            {
                NetworkRequest request = requestList[i];
                request.Update(deltaTime);
                if (request.State == NetworkRequest.RequestStates.Error ||
                    request.State == NetworkRequest.RequestStates.TimedOut)
                {
                    //TODO: GL - Error handler
                    requestList.Remove(request);
                }
                else if (request.State == NetworkRequest.RequestStates.Finished)
                {
                    requestList.Remove(request);
                }
                else
                {
                    switch (request.State)
                    {
                        case NetworkRequest.RequestStates.Initial:
                            DoSendRequest(request);
                            break;
                        case NetworkRequest.RequestStates.Processing:
                            //超时检查？还是用Pomelo检查超时？
                            if (request.timer > (float)RequestTimeout)
                            {
                                request.ChangeState(NetworkRequest.RequestStates.TimedOut);
                                OnRequestFinish(request);
                            }
                            break;
                    }
                }
            }
        }

        private void InnerDoResponse(NetworkRequest request)
        {
            //Debug.LogWarning("Request进队列:"+request.route);
            responseQueue.Enqueue(request);
        }

        protected void InnerEventTrigger(NetworkEvent networkEvent)
        {
            innerEventQueue.Enqueue(networkEvent);
        }

        private IEnumerator delayDoSendRequest(NetworkRequest request, float delay)
        {
            Debug.Log("延时:" + delay.ToString());
            yield return new WaitForSeconds(delay);
            this.DoSendRequest(request);
        }

        private void TriggerEvent(NetworkEvent networkEvent)
        {
            if (networkEvent != null)
            {
                networkEvent.DoAction();
            }
        }

        /// <summary>
        /// 连接状态处理
        /// </summary>
        /// <param name="param"></param>
        protected void NetWorkStateChangedHandler(object obj)
        {
            JsonObject param = obj as JsonObject;
            NetWorkState state = (NetWorkState)param["state"];
            //Debug.LogFormat("NetWork State Changed:{0} | Thread:{1}", state.ToString(), Thread.CurrentThread.ManagedThreadId);
            if (onWaiting != null)
            {
                onWaiting(false);
            }
            switch (state)
            {
                case NetWorkState.CLOSED:
                    if (onClose != null)
                    {
                        onClose(null);
                    }
                    // 未连接状态。
                    break;
                case NetWorkState.CONNECTED:
                    bool connected = false;
                    try
                    {
                        InnerConnect();
                    }
                    catch (Exception e)
                    {
                        OnConnectExceptionHandler(GetText("handshake_local_failed"));// "Pomelo protocol init(handshake) local failed!-" + e.ToString());
                    }
                    if (!connected)
                    {
                        OnConnectExceptionHandler(GetText("handshake_remote_failed"));// "Pomelo protocol init(handshake) remote failed!");
                    }
                    break;
                case NetWorkState.CONNECTING:
                    break;
                case NetWorkState.TIMEOUT:  //连接超时
                    OnConnectExceptionHandler(GetText("connect_timeout"));// string.Format("Connect timeout!host:{0}-port:{1}", this.host, this.port));
                    if (onConnectionTimeout != null)
                    {
                        onConnectionTimeout(null);
                    }
                    // 如果opening为true，则尝试重连
                    break;
                case NetWorkState.DISCONNECTED: // 如果收发消息失败，也会触发断开连接。
                    if (onClose != null)
                    {
                        JsonObject ret = new JsonObject();
                        onClose(ret);
                    }
                    // 如果opening为true，则尝试重连
                    OnConnectExceptionHandler(GetText("network_disconnected"));
                    break;
                case NetWorkState.ERROR:
                    OnConnectExceptionHandler(GetText("connect_error"));//string.Format("Connect error!host:{0}-port:{1}", this.host, this.port));
                    break;
            }
        }

        private void OnConnectExceptionHandler(string errorMessage)
        {
            Debug.LogWarning(errorMessage);
            if (opening)
            {
                if (onError != null)
                {
                    JsonObject param = new JsonObject();
                    param["error"] = errorMessage;
                    onError(param);
                }
                // 如果是后台尝试或者没有UI弹框，则自动重连。
                if (background || onDialog == null)
                {
                    // （持续）延时连接
                    CoroutineUtil.DoCoroutine(delayConnect(ReconnectionDelay));
                }
                else
                {
                    if (reconnectCounter >= AutoReconnectTimes)
                    {
                        if (onDialog != null)
                        {
                            onDialog(GetText("ws_network_error"), errorMessage, true, (bool retry) =>
                            {
                                // 不管点什么都重试
                                Debug.Log("[有UI]手动重连!");
                                CoroutineUtil.DoCoroutine(delayConnect(0.0f));
                            });
                        }
                        else
                        {
                            Debug.Log("[无UI]手动重连!");
                            CoroutineUtil.DoCoroutine(delayConnect(0.0f));
                        }
                        reconnectCounter = 0;
                    }
                    else
                    {
                        reconnectCounter++;
                        CoroutineUtil.DoCoroutine(delayConnect(ReconnectionDelay));
                    }
                }
            }
            else
            {
                // do nothing
            }
        }

        /// <summary>
        /// 消息请求的异常处理
        /// </summary>
        /// <param name="request"></param>
        private void OnRequestExceptionHandler(NetworkRequest request)
        {
            // 请求失败。
            if (onRequestError != null)
            {
                onRequestError(request);
            }
            //if (onDialog != null)
            //{
            //    onDialog(TextSystem.GetText("request-error"), request.errorMessage, true, (bool retry) => {
            //        if (opening) {
            //            // 延迟N秒
            //            CoroutineUtil.DoCoroutine(delayDoSendRequest(request, 1.0f));
            //        }
            //    });
            //}
        }
    }
}

