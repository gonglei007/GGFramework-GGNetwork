using System;
using System.Text;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    public class HTTPRequest
    {
        public enum States
        {
            Initial,
            Queued,
            Processing,
            Finished,
            Error,
            Aborted,
            ConnectionTimedOut,
            TimedOut
        }

        public HTTPRequest()
        {
        }

        //public HTTPRequest(HTTPRequest request) {
        //    this.request = request.request;
        //}

        public virtual Uri GetUri()
        {
            Debug.LogWarning("Must implement it!");
            return null;
        }

        public virtual Uri GetCurrentUri()
        {
            Debug.LogWarning("Must implement it!");
            return null;
        }

        public virtual States GetState()
        {
            Debug.LogWarning("Must implement it!");
            return States.Error;
        }

        public virtual string GetExceptionMessage()
        {
            return "";
        }

        public virtual HTTPRequest CreatePostRequest(Uri uri, string contentType, byte[] byteArray, HTTPForm form, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            Debug.LogError("Must implement it!");
            return null;
        }

        public virtual HTTPRequest CreateGetRequest(Uri uri, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
        {
            Debug.LogError("Must implement it!");
            return null;
        }

        public virtual void SendRequest()
        {
            Debug.LogWarning("Must implement it!");
        }

        public static HTTPRequest sCreatePostRequest(
            Uri uri,
            string paramString,
            string contentType,
            HTTPForm form = null,    // 临时参数，将来在系统内定义这个类型。
            HttpNetworkSystem.ExceptionAction exceptionAction = HttpNetworkSystem.ExceptionAction.ConfirmRetry,
            Action<JsonObject> callback = null)
        {
            HTTPRequest request = HttpNetworkSystem.Instance.HTTPFactory.CreateHTTPRequest(uri);
            //Debug.Log("++++>执行http线程");
            byte[] byteArray = Encoding.UTF8.GetBytes(paramString);
            
            request.CreatePostRequest(uri, contentType, byteArray, form, exceptionAction, callback);
            return request;
        }

        public static HTTPRequest sCreateGetRequest(
            Uri uri,
            HttpNetworkSystem.ExceptionAction exceptionAction = HttpNetworkSystem.ExceptionAction.ConfirmRetry,
            Action<JsonObject> callback = null)
        {
            HTTPRequest request = HttpNetworkSystem.Instance.HTTPFactory.CreateHTTPRequest(uri);
            request.CreateGetRequest(uri, exceptionAction, callback);
            return request;
        }

    }
}

