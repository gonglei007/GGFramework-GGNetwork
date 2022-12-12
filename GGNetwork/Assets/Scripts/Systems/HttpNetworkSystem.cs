using System;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP;
using System.Threading;
using SimpleJson;
using System.Text;
using GGFramework.GGTask;

namespace GGFramework.GGNetwork
{
    public class RequestItem
    {
        public HttpNetworkSystem.RequestType requestType;

        public string httpAddress;

        public string command;

        public string paramData;

        public Encoding dataEncode;

        public HttpNetworkSystem.ExceptionAction exceptionAction;

        public Action<JsonObject> callback;

        public bool isRetry = false;//此请求是否在自动重试

        public int retryNum = 1;//重试次数

        public float retryTime = 1.0f;//重试间隔

        public RequestItem(HttpNetworkSystem.RequestType requestType, string httpAddress, string command, string paramData, Encoding dataEncode, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            this.requestType = requestType;
            this.httpAddress = httpAddress;
            this.command = command;
            this.paramData = paramData;
            this.dataEncode = dataEncode;
            this.exceptionAction = exceptionAction;
            this.callback = callback;
        }
    }

    public static class HTTPRequestAdapter
    {
        private static Dictionary<HTTPRequest, string> serviceTypeMap = new Dictionary<HTTPRequest, string>();

        public static void SetServiceType(this HTTPRequest request, string serviceType)
        {
            serviceTypeMap[request] = serviceType;
            Debug.Log(">>set service type:" + request.Uri.ToString());
        }

        public static string GetServiceType(this HTTPRequest request)
        {
            if (request == null)
            {
                throw new Exception("Illegal http request!");
            }
            Debug.Log(">>get service type:" + request.Uri.ToString());
            if (!serviceTypeMap.ContainsKey(request))
            {
                return null;
                //throw new Exception("http request in map lost!!!");
            }
            return serviceTypeMap[request];
        }
    }

    /// <summary>
    /// Htpp请求的Manager。
    /// 实现了超时重试，暂时只确保一次一个请求。
    /// TODO: GL - 异常：1. 网络异常，弹出确认框；2. 服务器异常，弹出确认框，点击确定退出、以后做点击确认跳转官网。
    /// TODO: GL - 将来视需求来决定是否需要实现并行多个请求。
    /// TODO: CWW 1.将客户端逻辑错误从网络错误的try中分离 2.RequestItem改为队列模式 3.服务器错误弹窗 4.客户端网络错误分三种情况：
    /// TODO: 异常信息的文本Key
    /// (1)自动重试(斐波那契数列) 重试过程中跑马 (2)无响应 (3)弹窗反馈无按钮
    /// </summary>
    public class HttpNetworkSystem : Singleton<HttpNetworkSystem>
    {
        //TODO: GL - 从服务端获取、更新此参数
        public int HttpConnectTimeout = 8;
        public int HttpRequestTimeout = 10;

        public enum RequestType
        {
            Get,
            Post
        }

        public enum RequsetCallBackType
        {
            NeedRemoveItemsAddCallBack,
            RetryRequestCallback
        }

        public enum ExceptionAction
        {
            Ignore,
            // 忽略。如果回调中会自行处理异常情况，就使用Ignore类型。
            ConfirmRetry,
            // 弹出消息框，等玩家操作。
            AutoRetry,
            // 自动静默重试。  //TODO: GL - 先别用这个Type，这个可能会立即无限尝试。将来要改造成定时尝试（如果是在主线程，就放到Update中定时检测）。
            Silence,
            // 静默，只发，无UI响应。
            Tips,
        }

        Dictionary<string, string> serviceUrls = new Dictionary<string, string>();
        public Action<string, string, bool, Action<bool>> onDialog = null;
        public Func<string, string> onGetText = null;
        public Action<bool> onWaiting = null;
        public Action<int> onResponseError = null;
        public INetworkCallback onResponseErrorX = null;

        private string httpSecretKey = null;
        private string deviceUID = null;
        private string channel = null;
        private string clientVersion = null;

        public void Awake()
        {
            HTTPManager.Setup();
        }

        public void Init(string secretKey=null, string deviceUID = null, string channel = null, string clientVersion = null)
        {
            this.httpSecretKey = secretKey;
            this.deviceUID = deviceUID;
            this.channel = channel;
            this.clientVersion = clientVersion;
        }

        public void SetServiceUrl(string service, string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!result)
            {
                string errorMessage = "Illegal URL format!!!Url-" + url;
                GameDebugger.sPushLog(errorMessage);
                throw new Exception(errorMessage);
            }
            serviceUrls[service] = url;
        }

        public string GetServiceUrl(string service)
        {
            if (!serviceUrls.ContainsKey(service))
            {
                return null;
            }
            return serviceUrls[service];
        }

        public List<RequestItem> needRetryItems = new List<RequestItem>();

        private static string token = null;

        public bool windowRequest = false;//是否已有窗口请求，如有，等待玩家操作(不进行自动重连)

        public const float RETRY_TIME_DURATION = 3.0f;

        public float retryTimer = RETRY_TIME_DURATION;

        public static string Token
        {
            get
            {
                return token;
            }
            set
            {
                token = value;
            }
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

        private void DoSendRequest(HTTPRequest request)
        {
            if (onWaiting != null)
            {
                onWaiting(true);
            }
            TaskSystem.Instance.QueueJob(() => {
                Debug.LogFormat("[thread-{0}]开始请求:{1}", Thread.CurrentThread.ManagedThreadId, request.Uri.ToString());
                HTTPManager.SendRequest(request);
                return null;
            });
        }

        /// <summary>
        /// 此接口为底层接口。建议使用的时候使用应用层的接口，那里会根据接口类型来决定使用哪种异常响应。
        /// </summary>
        /// <param name="httpAddress"></param>
        /// <param name="command"></param>
        /// <param name="paramObject"></param>
        /// <param name="exceptionAction">应用层定义不同的请求接口，根据消息类型（游戏消息、内购消息、流程消息、帐号消息）来决定异常响应类型。</param>
        /// <param name="callback"></param>
        public HTTPRequest PostWebRequest(string httpAddress, string command, JsonObject paramObject, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            Debug.Assert(httpAddress != null && paramObject != null, "Illegal parameters!!!");
            paramObject["__timestamp"] = NetworkUtil.GetTimeStamp();
            paramObject["__sign"] = NetworkUtil.Sign(paramObject, this.httpSecretKey);
            return PostWebRequest(httpAddress, command, paramObject.ToString(), "application/json; charset=UTF-8", null, exceptionAction, callback);
        }

        public HTTPRequest PostWebRequest(string httpAddress, string command, string paramString, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            return PostWebRequest(httpAddress, command, paramString, "application/x-www-form-urlencoded; charset=UTF-8", null, exceptionAction, callback);
        }

        public HTTPRequest PostWebRequest(string httpAddress, string command, WWWForm form, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            return PostWebRequest(httpAddress, command, "", "application/x-www-form-urlencoded; charset=UTF-8", new BestHTTP.Forms.UnityForm(form), exceptionAction, callback);
        }

        private HTTPRequest PostWebRequest(string httpAddress, string command, string paramString, string contentType, BestHTTP.Forms.HTTPFormBase form = null, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            if (httpAddress == null)
            {
                Debug.LogWarning("http address is null!!!command-" + command);
                return null;
            }
            Debug.Assert(paramString != null, "Illegal parameters!!!");
            Uri baseUri = new Uri(httpAddress);
            //Debug.Log("ready to request:" + baseUri.ToString());

            //Debug.Log("++++>执行http线程");
            byte[] byteArray = Encoding.UTF8.GetBytes(paramString);
            HTTPRequest postRequest = new HTTPRequest(new Uri(baseUri, command));
            //TODO: 这个时间设定是给普通的post、get消息请求的。对于下载请求，需要另外设置超时。
            postRequest.ConnectTimeout = TimeSpan.FromSeconds(HttpConnectTimeout);
            postRequest.Timeout = TimeSpan.FromSeconds(HttpRequestTimeout);
            postRequest.MethodType = HTTPMethods.Post;
            postRequest.AddHeader("Content-Type", contentType);
            postRequest.AddHeader("Content-Length", byteArray.Length.ToString());
            if (HttpNetworkSystem.Token != null)
            {
                postRequest.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
            }

            postRequest.AddHeader("x-deviceId", this.deviceUID);
            postRequest.AddHeader("x-channel", this.channel);
            postRequest.AddHeader("x-version", this.clientVersion);
            if (form != null)
            {
                postRequest.SetForm(form);
            }
            postRequest.RawData = byteArray;
            postRequest.Callback = (HTTPRequest originalRequest, HTTPResponse response) => {
                OnRequestFinished(originalRequest, response, exceptionAction, callback);
            };
            DoSendRequest(postRequest);
            return postRequest;
        }

        public HTTPRequest GetWebRequest(string httpAddress, string command, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            if (httpAddress == null)
            {
                throw new Exception("Illegal null http address!!!");
            }
            command += "&__timestamp=" + NetworkUtil.GetTimeStamp().ToString();
            if (!string.IsNullOrEmpty(this.httpSecretKey)) {
                command += "&__sign=" + NetworkUtil.Sign(command, this.httpSecretKey);
            }
            GameDebugger.sPushLog(string.Format("url:{0} - command:{1}", httpAddress, command));
            Uri baseUri = new Uri(httpAddress);
            Debug.Log("Http get: base Uri->"+ baseUri.ToString());

            HTTPRequest postRequest = new HTTPRequest(new Uri(baseUri, command));
            postRequest.ConnectTimeout = TimeSpan.FromSeconds(HttpConnectTimeout);
            postRequest.Timeout = TimeSpan.FromSeconds(HttpRequestTimeout);
            postRequest.MethodType = HTTPMethods.Get;
            if (HttpNetworkSystem.Token != null)
            {
                postRequest.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
            }
            postRequest.AddHeader("x-deviceId", this.deviceUID);
            postRequest.AddHeader("x-channel", this.channel);
            postRequest.AddHeader("x-version", this.clientVersion);
            postRequest.Callback = (HTTPRequest originalRequest, HTTPResponse response) => {
                OnRequestFinished(originalRequest, response, exceptionAction, callback);
            };
            DoSendRequest(postRequest);
            return postRequest;
        }

        private void TriggerResponseError(int statusCode)
        {
            if (onResponseError != null)
            {
                onResponseError(statusCode);
            }
            if (onResponseErrorX != null)
            {
                onResponseErrorX.Call(statusCode.ToString());
            }
        }

        private void OnExceptionHandler(HTTPRequest originalRequest, string message, ExceptionAction exceptionAction)
        {
            if (exceptionAction == ExceptionAction.AutoRetry || exceptionAction == ExceptionAction.ConfirmRetry)
            {
                string serviceType = originalRequest.GetServiceType();
                ServiceCenter.Instance.RefereshServiceHost(serviceType, (string type, string host)=> {
                    SetServiceUrl(type, host);
                });
            }
            switch (exceptionAction)
            {
                case ExceptionAction.AutoRetry:
                    //TODO: GL - 先测试出来，那些情况会有非超时类的异常。超时类的异常本身时间比较长，所以不用自动重试。非超时类的异常如果比较频繁，再处理定时重试的功能。
                    //CommonTools.SetTimeout(2.0f, () => {
                    //});
                    if (onDialog != null)
                    {
                        onDialog(this.GetText("server_error"), message, true, (bool retry) => {
                            DoSendRequest(originalRequest);
                        });
                    }
                    break;
                case ExceptionAction.ConfirmRetry:
                    if (onDialog != null)
                    {
                        onDialog(this.GetText("server_error"), message, true, (bool retry) => {
                            DoSendRequest(originalRequest);
                        });
                    }
                    break;
                case ExceptionAction.Tips:
                    if (onDialog != null)
                    {
                        onDialog(this.GetText("server_error"), message, false, (bool retry) => {
                            Debug.LogFormat("[http-error]只告知异常:{0}", originalRequest.Uri.ToString());
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理HTTP返回的结果（和状态）。
        /// </summary>
        /// <param name="originalRequest"></param>
        /// <param name="response"></param>
        /// <param name="callback"></param>
        public void OnRequestFinished(HTTPRequest originalRequest, HTTPResponse response, ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            Debug.LogFormat("<=thread id:{0}", Thread.CurrentThread.ManagedThreadId);
            Debug.Log("http response:" + originalRequest.Uri.ToString());
            //string serviceType = originalRequest.GetServiceType();
            //Debug.LogFormat(">>>>>>>>>>>>>>>>>请求返回, service type:{0}：", serviceType);
            if (onWaiting != null)
            {
                onWaiting(false);
            }
            switch (originalRequest.State)
            {
                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (response.IsSuccess)
                    {
                        // Everything went as expected!
                        if (response == null)
                        {
                            Debug.LogWarning("http response is null!");
                            return;
                        }
                        Debug.Log("http response status:" + response.StatusCode.ToString());
                        //Debug.Log("http response data:" + response.DataAsText);
                        if (callback != null)
                        {
                            Debug.LogFormat("==:thread id:{0}", Thread.CurrentThread.ManagedThreadId);
                            JsonObject responseObj = null;
                            int code = NetworkConst.CODE_FAILED;
                            try
                            {
                                responseObj = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(response.DataAsText);
                                code = Convert.ToInt32(responseObj["code"]);
                                if (code != NetworkConst.CODE_OK)
                                {
                                    if (onDialog != null)
                                    {
                                        string message = responseObj["msg"].ToString();
                                        onDialog(this.GetText("server_warning"), message, true, (bool retry) =>
                                        {
                                            //DoSendRequest(originalRequest);
                                        });
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                // 由于是后端的错误（严重错误），直接弹框，让后端去解决此问题。
                                OnExceptionHandler(originalRequest, this.GetText("ask-server-developer-to-fix" + e.ToString()), ExceptionAction.ConfirmRetry);
                            }
                            finally
                            {
                                // 发生异常情况就不进行回调了。
                                if (responseObj != null)
                                {
                                    callback(responseObj);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("Request finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        response.StatusCode,
                                                        response.Message,
                                                        response.DataAsText));
                        if (response.StatusCode != 200)
                        {
                            TriggerResponseError(response.StatusCode);
                            OnExceptionHandler(originalRequest, this.GetText(string.Format("Error Status Code:\n{0};\n{1};\n{2}.", response.StatusCode.ToString(), response.Message, response.DataAsText)), ExceptionAction.ConfirmRetry);
                            return;
                        }
                        //OnExceptionHandler(originalRequest, TextSystem.GetText("Error Status Code:\n{0};\n{1};\n{2}.", response.StatusCode.ToString(), response.Message, response.DataAsText), ExceptionAction.Tips);
                        //因为后端判断有严重异常，所以处理异常后不要再继续调callback了。
                    }
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    Debug.LogError("Request Finished with Error! " + (originalRequest.Exception != null ? (originalRequest.Exception.Message + "\n" + originalRequest.Exception.StackTrace) : "No Exception"));
                    OnExceptionHandler(originalRequest, this.GetText("Connection Timed Out!"), exceptionAction);
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    //TODO:GL
                    Debug.LogWarning("Request Aborted!");
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    Debug.LogError("Connection Timed Out!");
                    if (exceptionAction == ExceptionAction.AutoRetry)
                    {
                        exceptionAction = ExceptionAction.ConfirmRetry;
                    }
                    OnExceptionHandler(originalRequest, this.GetText("Connection Timed Out!"), exceptionAction);
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    Debug.LogError("Processing the request Timed Out!");
                    if (exceptionAction == ExceptionAction.AutoRetry)
                    {
                        exceptionAction = ExceptionAction.ConfirmRetry;
                    }
                    OnExceptionHandler(originalRequest, this.GetText("Processing the request Timed Out!"), exceptionAction);
                    break;
            }
        }

        public void PostServiceRequest(string service, string command, JsonObject jsonParams, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            string url = GetServiceUrl(service);
            if (url == null)
            {
                throw new Exception(string.Format("Illegal service URL!!!service:{0}", service));
            }
            HTTPRequest request = PostWebRequest(url, command, jsonParams, exceptionAction, callback);
            request.SetServiceType(service);
        }

        /// <summary>
        /// 此方法主要是给Lua调用使用的（虽然它支持CS调用）。
        /// </summary>
        /// <param name="service"></param>
        /// <param name="command"></param>
        /// <param name="jsonParams"></param>
        /// <param name="exceptionAction"></param>
        /// <param name="callback"></param>
        public void PostServiceRequest(string service, string command, string jsonParams, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<string> callback = null)
        {
            try
            {
                JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonParams);
                PostServiceRequest(service, command, jsonObject, exceptionAction, (JsonObject responseObject) => {
                    callback(responseObject.ToString());
                });
            }
            catch (Exception e)
            {
                GameDebugger.sPushLog("Web post error!" + e.ToString());
            }
        }

        /// <summary>
        /// 这个接口是用于给Lua传输数据的。
        /// TODO: GL - 将来要做优化，不要用jsonobject转string，而是直接获得string！！！
        /// </summary>
        /// <param name="service"></param>
        /// <param name="command"></param>
        /// <param name="jsonParams"></param>
        /// <param name="exceptionAction"></param>
        /// <param name="callback"></param>
        public void PostServiceRequestX(string service, string command, string jsonParams, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, INetworkCallback callback = null)
        {
            Debug.Assert(callback != null, "callback will not be null!!!");
            PostServiceRequest(service, command, jsonParams, exceptionAction, (string response) => {
                callback.Call(response);
            });
        }

        public void GetServiceRequest(string service, string command, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            string url = GetServiceUrl(service);
            if (url == null)
            {
                throw new Exception(string.Format("Illegal service URL!!!service:{0}", service));
            }

            GetWebRequest(url, command, exceptionAction, callback);
        }

        public void GetServiceRequestX(string service, string command, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, INetworkCallback callback = null)
        {
            Debug.Assert(callback != null, "callback will not be null!!!");
            GetServiceRequest(service, command, exceptionAction, (JsonObject response) => {
                callback.Call(response.ToString());
            });
        }

        /// <summary>
        /// 重复发送http请求(按照斐波那契数列)
        /// </summary>
        /// <param name="deltaTime"></param>
        public void LoopRequest(float deltaTime)
        {
            for (int i = 0; i < needRetryItems.Count; i++)
            {
                needRetryItems[i].retryTime -= deltaTime;
                if (needRetryItems[i].retryTime <= 0.0f)
                {
                    //NetworkWaitingMask.ShowMask(false, "httpMask");
                    needRetryItems[i].retryNum = (needRetryItems[i].retryNum + 1) % 5;
                    //int tempIndex = (needRetryItems[i].retryNum % 5) + 1;// + 1是因为斐波那契数列没有0的选项
                    int tempIndex = needRetryItems[i].retryNum;
                    needRetryItems[i].retryTime = GetWaitTime(tempIndex);
                    //needRetryItems[i].retryNum++;
                    if (needRetryItems[i].retryNum % 5 == 0)
                    {
                        windowRequest = true;
                    }
                    else
                    {
                        //HttpThread.Instance.AddWebRequest(needRetryItems[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 根据调用次数获取等待时间。
        /// </summary>
        /// <param name="callTimes"></param>
        /// <returns></returns>
        float GetWaitTime(int callTimes)
        {
            float[] waitTimes = new float[] { 1.0f, 2.0f, 3.0f, 5.0f, 5.0f };
            int index = callTimes;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= waitTimes.Length)
            {
                index = waitTimes.Length - 1;
            }
            return waitTimes[index];
        }


        public void Update()
        {
            //if (needRetryItems != null && needRetryItems.Count > 0)
            //{
            //    if (!windowRequest)
            //    {
            //        LoopRequest(Time.deltaTime);
            //    }
            //}
        }
    }
}
