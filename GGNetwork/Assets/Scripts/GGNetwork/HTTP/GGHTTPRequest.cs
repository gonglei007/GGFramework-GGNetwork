using System;
using System.Text;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    public class GGHTTPRequest
    {
        public BestHTTP.HTTPRequest request;

        public GGHTTPRequest()
        {
        }

        public GGHTTPRequest(BestHTTP.HTTPRequest request)
        {
            this.request = request;
        }

        public GGHTTPRequest(GGHTTPRequest request) {
            this.request = request.request;
        }

        public static GGHTTPRequest CreatePostRequest(
            Uri uri,
            string paramString,
            string contentType,
            BestHTTP.Forms.HTTPFormBase form = null,    // 临时参数，将来在系统内定义这个类型。
            HttpNetworkSystem.ExceptionAction exceptionAction = HttpNetworkSystem.ExceptionAction.ConfirmRetry,
            Action<JsonObject> callback = null)
        {
            GGHTTPRequest request = new GGHTTPRequest();
            //Debug.Log("++++>执行http线程");
            byte[] byteArray = Encoding.UTF8.GetBytes(paramString);
            BestHTTP.HTTPRequest postRequest = new BestHTTP.HTTPRequest(uri);
            //TODO: 这个时间设定是给普通的post、get消息请求的。对于下载请求，需要另外设置超时。
            postRequest.ConnectTimeout = TimeSpan.FromSeconds(ServiceCenter.HttpConnectTimeout);
            postRequest.Timeout = TimeSpan.FromSeconds(ServiceCenter.HttpRequestTimeout);
            postRequest.MethodType = BestHTTP.HTTPMethods.Post;
            postRequest.AddHeader("Content-Type", contentType);
            postRequest.AddHeader("Content-Length", byteArray.Length.ToString());
            if (HttpNetworkSystem.Token != null)
            {
                postRequest.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
            }

            if (NetworkConst.deviceUID != null)
            {
                postRequest.AddHeader("x-deviceId", NetworkConst.deviceUID);
            }
            if (NetworkConst.channel != null)
            {
                postRequest.AddHeader("x-channel", NetworkConst.channel);
            }
            if (NetworkConst.clientVersion != null)
            {
                postRequest.AddHeader("x-version", NetworkConst.clientVersion);
            }
            if (form != null)
            {
                postRequest.SetForm(form);
            }
            postRequest.RawData = byteArray;
            postRequest.Callback = (BestHTTP.HTTPRequest originalRequest, BestHTTP.HTTPResponse response) => {
                if (callback != null) {
                    GGHTTPResponse newResponse = new GGHTTPResponse(response);
                    HttpNetworkSystem.Instance.OnRequestFinished(request, newResponse, exceptionAction, callback);
                }
            };
            request.request = postRequest;
            return request;
        }

        public static GGHTTPRequest CreateGetRequest(
            Uri uri,
            HttpNetworkSystem.ExceptionAction exceptionAction = HttpNetworkSystem.ExceptionAction.ConfirmRetry,
            Action<JsonObject> callback = null)
        {
            GGHTTPRequest request = new GGHTTPRequest();

            BestHTTP.HTTPRequest postRequest = new BestHTTP.HTTPRequest(uri);
            postRequest.ConnectTimeout = TimeSpan.FromSeconds(ServiceCenter.HttpConnectTimeout);
            postRequest.Timeout = TimeSpan.FromSeconds(ServiceCenter.HttpRequestTimeout);
            postRequest.MethodType = BestHTTP.HTTPMethods.Get;
            if (HttpNetworkSystem.Token != null)
            {
                postRequest.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
            }
            if (NetworkConst.deviceUID != null)
            {
                postRequest.AddHeader("x-deviceId", NetworkConst.deviceUID);
            }
            if (NetworkConst.channel != null)
            {
                postRequest.AddHeader("x-channel", NetworkConst.channel);
            }
            if (NetworkConst.clientVersion != null)
            {
                postRequest.AddHeader("x-version", NetworkConst.clientVersion);
            }
            postRequest.Callback = (BestHTTP.HTTPRequest originalRequest, BestHTTP.HTTPResponse response) => {
                if (callback != null)
                {
                    GGHTTPResponse newResponse = new GGHTTPResponse(response);
                    HttpNetworkSystem.Instance.OnRequestFinished(request, newResponse, exceptionAction, callback);
                }
            };
            request.request = postRequest;
            return request;
        }

    }
}

