using System;
using SimpleJson;
using UnityEngine;
using GGFramework.GGNetwork;

public class BestHTTPRequest : HTTPRequest
{
    public BestHTTP.HTTPRequest request = null;

    public BestHTTPRequest(Uri uri){
        request = new BestHTTP.HTTPRequest(uri);
    }

    public BestHTTPRequest(BestHTTP.HTTPRequest request)
    {
        this.request = request;
    }

    public override Uri GetUri()
    {
        return request.Uri;
    }

    public override Uri GetCurrentUri()
    {
        return request.CurrentUri;
    }

    public override States GetState()
    {
        //TODO: 做安全的转换。
        return (HTTPRequest.States)request.State;
    }

    public override string GetExceptionMessage()
    {
        //string exceptionMessage = request.Exception != null ? request.Exception.Message : "No Exception";
        string exceptionMessage = "Request Finished with Error! " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception");
        Debug.LogError(exceptionMessage);
        return exceptionMessage;
    }

    public override HTTPRequest CreatePostRequest(Uri uri, string contentType, byte[] byteArray, HTTPForm form, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
    {
        base.CreatePostRequest(uri, contentType, byteArray, form, exceptionAction, callback);

        request = new BestHTTP.HTTPRequest(uri);
        //TODO: 这个时间设定是给普通的post、get消息请求的。对于下载请求，需要另外设置超时。
        request.ConnectTimeout = TimeSpan.FromSeconds(ServiceCenter.HttpConnectTimeout);
        request.Timeout = TimeSpan.FromSeconds(ServiceCenter.HttpRequestTimeout);
        request.MethodType = BestHTTP.HTTPMethods.Post;
        request.AddHeader("Content-Type", contentType);
        request.AddHeader("Content-Length", byteArray.Length.ToString());
        if (HttpNetworkSystem.Token != null)
        {
            request.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
        }

        if (NetworkConst.deviceUID != null)
        {
            request.AddHeader("x-deviceId", NetworkConst.deviceUID);
        }
        if (NetworkConst.channel != null)
        {
            request.AddHeader("x-channel", NetworkConst.channel);
        }
        if (NetworkConst.clientVersion != null)
        {
            request.AddHeader("x-version", NetworkConst.clientVersion);
        }
        if (form != null)
        {
            BestHTTP.Forms.UnityForm bestForm = new BestHTTP.Forms.UnityForm(form.GetUnityForm());
            request.SetForm(bestForm);
        }
        request.RawData = byteArray;
        request.Callback = (BestHTTP.HTTPRequest originalRequest, BestHTTP.HTTPResponse response) => {
            if (callback != null) {
                BestHTTPResponse newResponse = new BestHTTPResponse(response);
                HttpNetworkSystem.Instance.OnRequestFinished(this, newResponse, exceptionAction, callback);
            }
        };
        return this;
    }

    public override HTTPRequest CreateGetRequest(Uri uri, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
    {
        request = new BestHTTP.HTTPRequest(uri);
        request.ConnectTimeout = TimeSpan.FromSeconds(ServiceCenter.HttpConnectTimeout);
        request.Timeout = TimeSpan.FromSeconds(ServiceCenter.HttpRequestTimeout);
        request.MethodType = BestHTTP.HTTPMethods.Get;
        if (HttpNetworkSystem.Token != null)
        {
            request.AddHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
        }
        if (NetworkConst.deviceUID != null)
        {
            request.AddHeader("x-deviceId", NetworkConst.deviceUID);
        }
        if (NetworkConst.channel != null)
        {
            request.AddHeader("x-channel", NetworkConst.channel);
        }
        if (NetworkConst.clientVersion != null)
        {
            request.AddHeader("x-version", NetworkConst.clientVersion);
        }
        request.Callback = (BestHTTP.HTTPRequest originalRequest, BestHTTP.HTTPResponse response) => {
            if (callback != null)
            {
                BestHTTPResponse newResponse = new BestHTTPResponse(response);
                HttpNetworkSystem.Instance.OnRequestFinished(this, newResponse, exceptionAction, callback);
            }
        };
        return this;
    }

    public override void SendRequest()
    {
        BestHTTP.HTTPManager.SendRequest(request);
    }
}
