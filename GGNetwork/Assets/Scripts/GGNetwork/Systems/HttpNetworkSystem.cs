using System;
using System.Collections.Generic;
using UnityEngine;
//using BestHTTP;
using System.Threading;
using SimpleJson;
using System.Text;
using GGFramework.GGTask;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// Http网络系统，是Htpp请求的总入口。
    /// 实现了超时重试，暂时只确保一次一个请求。
    /// 异常处理：1. 网络异常，弹出确认框；2. 服务器异常，弹出确认框，点击确定退出、以后做点击确认跳转官网。
    /// TODO: GL - 将来视需求来决定是否需要实现并行多个请求。
    /// TODO: CWW 1.将客户端逻辑错误从网络错误的try中分离 2.RequestItem改为队列模式 3.服务器错误弹窗 4.客户端网络错误分三种情况：
    /// TODO: 异常信息的文本Key
    /// (1)自动重试(斐波那契数列) 重试过程中跑马 (2)无响应 (3)弹窗反馈无按钮
    /// </summary>
    public class HttpNetworkSystem : Singleton<HttpNetworkSystem>
    {
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

        public enum EParamType
        {
            Text,
            Json,
        }

        Dictionary<string, string> serviceUrls = new Dictionary<string, string>();
        private UIAdaptor uiAdaptor = new UIAdaptor();
        public UIAdaptor UIAdaptor
        {
            get
            {
                return uiAdaptor;
            }
        }
        private LogAdaptor logAdaptor = new LogAdaptor();
        public LogAdaptor LogAdaptor
        {
            get
            {
                return logAdaptor;
            }
        }
        public Action<int> onResponseError = null;
        public INetworkCallback onResponseErrorX = null;

        EParamType paramType = EParamType.Json;
        public EParamType ParamType {
            set {
                paramType = value;
            }
            get {
                return paramType;
            }
        }

        private bool enablePreProcessParam = false;    // 是否开启参数预处理，如果开启，会在参数前加入签名等。这就需要服务端支持。
        public bool EnablePreProcessParam
        {
            set
            {
                enablePreProcessParam = true;
            }
            get
            {
                return enablePreProcessParam;
            }
        }

        private bool enableHttpDNS = false;     // 是否开启Http DNS，如果开启，请求网络的时候，先把host变成ip（缓存起来），再请求。
        public bool EnableHttpDNS
        {
            set
            {
                enableHttpDNS = true;
            }
            get
            {
                return enableHttpDNS;
            }
        }

        public void Awake()
        {
            //HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All;
            BestHTTP.HTTPManager.Setup();
        }

        public void Init()
        {
            //TODO: 
        }

        /// <summary>
        /// 暂不开放
        /// </summary>
        /// <param name="service"></param>
        /// <param name="url"></param>
        private void SetServiceUrl(string service, string url)
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

        /// <summary>
        /// 暂不开放
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        private string GetServiceUrl(string service)
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

        private void DoSendRequest(GGHTTPRequest request)
        {
            uiAdaptor.ShowWaiting(true);
            TaskSystem.Instance.QueueJob(() => {
                Debug.LogFormat("[thread-{0}]开始请求:{1}", Thread.CurrentThread.ManagedThreadId, request.request.Uri.ToString());
                BestHTTP.HTTPManager.SendRequest(request.request);
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
        public GGHTTPRequest PostWebRequest(string httpAddress, string command, JsonObject paramObject, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            Debug.Assert(httpAddress != null && paramObject != null, "Illegal parameters!!!");
            PreProcessParam(paramObject);
            return PostWebRequest(httpAddress, command, paramObject.ToString(), "application/json; charset=UTF-8", null, exceptionAction, callback);
        }

        public GGHTTPRequest PostWebRequest(string httpAddress, string command, string paramString, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            return PostWebRequest(httpAddress, command, paramString, "application/x-www-form-urlencoded; charset=UTF-8", null, exceptionAction, callback);
        }

        public GGHTTPRequest PostWebRequest(string httpAddress, string command, WWWForm form, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            return PostWebRequest(httpAddress, command, "", "application/x-www-form-urlencoded; charset=UTF-8", new BestHTTP.Forms.UnityForm(form), exceptionAction, callback);
        }

        private GGHTTPRequest PostWebRequest(string httpAddress, string command, string paramString, string contentType, BestHTTP.Forms.HTTPFormBase form = null, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            if (httpAddress == null)
            {
                throw new Exception("Illegal null http address!!!");
            }
            Debug.Assert(paramString != null, "Illegal parameters!!!");
            if (this.enableHttpDNS) {
                httpAddress = ServiceCenter.Instance.HTTPDNS.GetURLByIP(httpAddress);
            }
            Uri baseUri = new Uri(httpAddress);
            //Debug.Log("ready to request:" + baseUri.ToString());

            GGHTTPRequest postRequest = GGHTTPRequest.CreatePostRequest(
                new Uri(baseUri, command),
                paramString,
                contentType,
                form,
                exceptionAction,
                callback
                );

            DoSendRequest(postRequest);
            return postRequest;
        }

        public GGHTTPRequest GetWebRequest(string httpAddress, string command, ExceptionAction exceptionAction = ExceptionAction.ConfirmRetry, Action<JsonObject> callback = null)
        {
            if (httpAddress == null)
            {
                throw new Exception("Illegal null http address!!!");
            }
            if (this.enableHttpDNS)
            {
                httpAddress = ServiceCenter.Instance.HTTPDNS.GetURLByIP(httpAddress);
            }
            command = PreProcessParam(command);
            GameDebugger.sPushLog(string.Format("url:{0} - command:{1}", httpAddress, command));

            Uri baseUri = new Uri(httpAddress);
            Debug.Log("Http get: base Uri->" + baseUri.ToString());

            GGHTTPRequest request = GGHTTPRequest.CreateGetRequest(
                new Uri(baseUri, command),
                exceptionAction,
                callback
                );

            DoSendRequest(request);
            return request;
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

        private void OnExceptionHandler(GGHTTPRequest originalRequest, string message, ExceptionAction exceptionAction)
        {
            //TODO: 可以考虑出错以后更改线路，以后再实现。
            //if (exceptionAction == ExceptionAction.AutoRetry || exceptionAction == ExceptionAction.ConfirmRetry)
            //{
            //    string host = originalRequest.request.Uri.ToString();
            //    ServiceCenter.Instance.HTTPDNS.parseIP(host, (string ip, HTTPDNS.EStatus status)=> {
            //        if (status == HTTPDNS.EStatus.RET_SUCCESS)
            //        {
            //            Uri uri = new Uri(ip);
            //            //originalRequest = new HTTPRequest(uri);
            //        }
            //        else {
            //            message = string.Format("Can not parse URL({0})!", host);
            //        }
            //    });
            //    //string serviceType = originalRequest.GetServiceType();
            //    //ServiceCenter.Instance.RefereshServiceHost(serviceType, (string type, string host)=> {
            //    //    SetServiceUrl(type, host);
            //    //});
            //}
            if (this.uiAdaptor == null) {
                // 如果没有UIAdaptor,就不走重试流程，直接走提示，然后结束。
                exceptionAction = ExceptionAction.Tips;
            }
            switch (exceptionAction)
            {
                case ExceptionAction.AutoRetry:
                    //TODO: GL - 先测试出来，那些情况会有非超时类的异常。超时类的异常本身时间比较长，所以不用自动重试。非超时类的异常如果比较频繁，再处理定时重试的功能。
                    //CommonTools.SetTimeout(2.0f, () => {
                    //});
                    uiAdaptor.ShowDialog("server_error", message, true, (bool retry) => {
                        DoSendRequest(originalRequest);
                    });
                    break;
                case ExceptionAction.ConfirmRetry:
                    uiAdaptor.ShowDialog("server_error", message, true, (bool retry) => {
                        DoSendRequest(originalRequest);
                    });
                    break;
                case ExceptionAction.Tips:
                    uiAdaptor.ShowDialog("server_error", message, false, (bool retry) => {
                        //TODO: 通过uiAdaptor.ShowInfo(Type[tip/dialog])来展示信息。
                        Debug.LogFormat("[http-error]只告知异常:{0}", originalRequest.request.Uri.ToString());
                    });
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
        public void OnRequestFinished(GGHTTPRequest originalRequest, GGHTTPResponse response, ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            //Debug.LogFormat("<=thread id:{0}", Thread.CurrentThread.ManagedThreadId);
            Debug.Log("originalRequest.State:" + originalRequest.request.State.ToString());
            //string serviceType = originalRequest.GetServiceType();
            //Debug.LogFormat(">>>>>>>>>>>>>>>>>请求返回, service type:{0}：", serviceType);
            uiAdaptor.ShowWaiting(false);
            // Everything went as expected!
            //if (response == null)
            //{
            //    Debug.LogWarning("http response is null!");
            //    OnExceptionHandler(originalRequest, string.Format("Http response is null!Fix it on server side!"), ExceptionAction.ConfirmRetry);
            //    return;
            //}
            switch (originalRequest.request.State)
            {
                // The request finished without any problem.
                case BestHTTP.HTTPRequestStates.Finished:
                    if (response.response.IsSuccess)
                    {
                        Debug.Log("http response:" + response.response.DataAsText.ToString());
                        Debug.Log("http response status:" + response.response.StatusCode.ToString());
                        //Debug.Log("http response data:" + response.DataAsText);

                        if (response.response.StatusCode != NetworkConst.CODE_OK)
                        {
                            string errorMessage = string.Format("Server side error-[{0}]", response.response.StatusCode);
                            Debug.LogError(errorMessage);
                            //originalRequest.request.Reset();
                            OnExceptionHandler(originalRequest, errorMessage, exceptionAction);
                            break;
                        }
                        if (callback != null)
                        {
                            //Debug.LogFormat("==:thread id:{0}", Thread.CurrentThread.ManagedThreadId);
                            int code = NetworkConst.CODE_FAILED;
                            JsonObject responseObj = null;
                            try
                            {
                                string message = null;
                                if (this.ParamType == EParamType.Json)
                                {
                                    responseObj = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(response.response.DataAsText);
                                    if (responseObj.ContainsKey("code"))
                                    {
                                        code = Convert.ToInt32(responseObj["code"]);
                                    }
                                    if (code != NetworkConst.CODE_OK)
                                    {
                                        message = responseObj["msg"].ToString();
                                        uiAdaptor.ShowDialog("server_warning", message, true, (bool retry) =>
                                        {
                                            //DoSendRequest(originalRequest);
                                        });
                                    }
                                }
                                else if (this.ParamType == EParamType.Text) {
                                    responseObj = new JsonObject();
                                    responseObj["response"] = response.response.DataAsText;
                                }

                            }
                            catch (Exception e)
                            {
                                // 由于是后端的错误（严重错误），直接弹框，让后端去解决此问题。
                                string errorDetail = string.Format("Response message parse error!-{0}", response.response.DataAsText);
                                Debug.LogError(errorDetail);
                                OnExceptionHandler(originalRequest, string.Format("[{0}]ask-server-developer-to-fix. {1}", NetworkConst.CODE_RESPONSE_MSG_ERROR, e.ToString()), ExceptionAction.ConfirmRetry);
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
                                                        response.response.StatusCode,
                                                        response.response.Message,
                                                        response.response.DataAsText));
                        if (response.response.StatusCode != NetworkConst.CODE_OK)
                        {
                            TriggerResponseError(response.response.StatusCode);
                            OnExceptionHandler(originalRequest, string.Format("Error Status Code:\n{0};\n{1};\n{2}.", response.response.StatusCode.ToString(), response.response.Message, response.response.DataAsText), ExceptionAction.ConfirmRetry);
                            return;
                        }
                        //OnExceptionHandler(originalRequest, TextSystem.GetText("Error Status Code:\n{0};\n{1};\n{2}.", response.StatusCode.ToString(), response.Message, response.DataAsText), ExceptionAction.Tips);
                        //因为后端判断有严重异常，所以处理异常后不要再继续调callback了。
                    }
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case BestHTTP.HTTPRequestStates.Error:
                    string ExceptionMessage = originalRequest.request.Exception != null ? originalRequest.request.Exception.Message : "No Exception";
                    Debug.LogError("Request Finished with Error! " + (originalRequest.request.Exception != null ? (originalRequest.request.Exception.Message + "\n" + originalRequest.request.Exception.StackTrace) : "No Exception"));
                    OnExceptionHandler(originalRequest, ExceptionMessage, exceptionAction);
                    break;

                // The request aborted, initiated by the user.
                case BestHTTP.HTTPRequestStates.Aborted:
                    //TODO:GL
                    Debug.LogWarning("Request Aborted!");
                    break;

                // Connecting to the server is timed out.
                case BestHTTP.HTTPRequestStates.ConnectionTimedOut:
                    Debug.LogError("Connection Timed Out!");
                    if (exceptionAction == ExceptionAction.AutoRetry)
                    {
                        exceptionAction = ExceptionAction.ConfirmRetry;
                    }
                    OnExceptionHandler(originalRequest, "Connection Timed Out!", exceptionAction);
                    break;

                // The request didn't finished in the given time.
                case BestHTTP.HTTPRequestStates.TimedOut:
                    Debug.LogError("Processing the request Timed Out!");
                    if (exceptionAction == ExceptionAction.AutoRetry)
                    {
                        exceptionAction = ExceptionAction.ConfirmRetry;
                    }
                    OnExceptionHandler(originalRequest, "Processing the request Timed Out!", exceptionAction);
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
            GGHTTPRequest request = PostWebRequest(url, command, jsonParams, exceptionAction, callback);
            //request.SetServiceType(service);
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

        private void PreProcessParam(JsonObject paramObject)
        {
            if (this.EnablePreProcessParam)
            {
                paramObject["__timestamp"] = NetworkUtil.GetTimeStamp();
                paramObject["__sign"] = NetworkUtil.Sign(paramObject, NetworkConst.httpSecretKey);
            }
        }

        private string PreProcessParam(string command)
        {
            if (this.EnablePreProcessParam)
            {
                if (!string.IsNullOrEmpty(command))
                {
                    command += "&__timestamp=" + NetworkUtil.GetTimeStamp().ToString();
                }
                if (!string.IsNullOrEmpty(NetworkConst.httpSecretKey))
                {
                    command += "&__sign=" + NetworkUtil.Sign(command, NetworkConst.httpSecretKey);
                }
            }
            return command;
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
