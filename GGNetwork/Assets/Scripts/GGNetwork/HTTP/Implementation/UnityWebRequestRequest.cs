using System;
using System.Collections;
using SimpleJson;
using UnityEngine;
using UnityEngine.Networking;
using GGFramework.GGNetwork;

/// <summary>
/// 基于 UnityWebRequest 的 HTTP 请求实现。
///
/// 对应 BestHTTPRequest，将 BestHTTP 对游戏有用的能力（超时、Header 注入、
/// 状态映射、异常信息、Form 上传）用 Unity 原生 UnityWebRequest 实现。
///
/// 线程模型：
///   - CreatePostRequest / CreateGetRequest：在调用线程（通常主线程）创建并配置 webRequest。
///   - SendRequest：可能被 HttpNetworkSystem 在子线程调用，因此不自接发送，
///     而是经 HttpRequestDriver 投递到主线程，用协程执行 SendWebRequest()。
///   - 请求完成后在主线程触发 HttpNetworkSystem.OnRequestFinished（框架统一处理
///     成功/错误/超时，并回调业务）。
/// </summary>
internal class UnityWebRequestRequest : HTTPRequest
{
    private UnityWebRequest webRequest;
    private Uri originalUri;
    private States state = States.Initial;
    private string exceptionMessage;
    private HttpNetworkSystem.ExceptionAction exceptionAction;
    private Action<JsonObject> callback;

    public UnityWebRequestRequest(Uri uri)
    {
        this.originalUri = uri;
    }

    public override Uri GetUri()
    {
        return originalUri;
    }

    public override Uri GetCurrentUri()
    {
        if (webRequest != null && !string.IsNullOrEmpty(webRequest.url))
        {
            try { return new Uri(webRequest.url); } catch { }
        }
        return originalUri;
    }

    public override States GetState()
    {
        return state;
    }

    public override string GetExceptionMessage()
    {
        if (string.IsNullOrEmpty(exceptionMessage)) exceptionMessage = "Request Finished with Error! No Exception";
        return exceptionMessage;
    }

    public override HTTPRequest CreatePostRequest(Uri uri, string contentType, byte[] byteArray, HTTPForm form, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
    {
        this.originalUri = uri;
        this.exceptionAction = exceptionAction;
        this.callback = callback;

        if (webRequest != null) { webRequest.Dispose(); webRequest = null; }

        if (form != null)
        {
            // 使用 WWWForm（multipart/form-data）
            WWWForm unityForm = form.GetUnityForm();
            if (unityForm != null)
            {
                webRequest = UnityWebRequest.Post(uri, unityForm);
            }
            else
            {
                webRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
                webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(contentType)) webRequest.SetRequestHeader("Content-Type", contentType);
            }
        }
        else
        {
            webRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(byteArray);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(contentType)) webRequest.SetRequestHeader("Content-Type", contentType);
        }

        ApplyCommon(uri);
        return this;
    }

    public override HTTPRequest CreateGetRequest(Uri uri, HttpNetworkSystem.ExceptionAction exceptionAction, Action<JsonObject> callback)
    {
        this.originalUri = uri;
        this.exceptionAction = exceptionAction;
        this.callback = callback;

        if (webRequest != null) { webRequest.Dispose(); webRequest = null; }

        webRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET);
        webRequest.downloadHandler = new DownloadHandlerBuffer();

        ApplyCommon(uri);
        return this;
    }

    /// <summary>公共配置：超时 + 通用 Header 注入 + 确保下载处理器。</summary>
    private void ApplyCommon(Uri uri)
    {
        // 必须提供 DownloadHandler，否则 responseCode/downlboardHandler 为空、GetData() 空。
        if (webRequest != null && webRequest.downloadHandler == null)
        {
            webRequest.downloadHandler = new DownloadHandlerBuffer();
        }

        // Unity 原生只提供一个整体超时（秒）。用 HttpRequestTimeout；<=0 表示不超时。
        webRequest.timeout = ServiceCenter.HttpRequestTimeout > 0 ? ServiceCenter.HttpRequestTimeout : 0;

        if (HttpNetworkSystem.Token != null)
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + HttpNetworkSystem.Token);
        }
        if (NetworkConst.deviceUID != null)
        {
            webRequest.SetRequestHeader("x-deviceId", NetworkConst.deviceUID);
        }
        if (NetworkConst.channel != null)
        {
            webRequest.SetRequestHeader("x-channel", NetworkConst.channel);
        }
        if (NetworkConst.clientVersion != null)
        {
            webRequest.SetRequestHeader("x-version", NetworkConst.clientVersion);
        }
    }

    public override void SendRequest()
    {
        // 可能被子线程调用（HttpNetworkSystem.DoSendRequest 在 TaskSystem 中）。
        // 把真正的发送协程投递到主线程。
        HttpRequestDriver.Enqueue(() =>
        {
            HttpRequestDriver.Instance.StartCoroutineSafe(SendCoroutine());
        });
    }

    private IEnumerator SendCoroutine()
    {
        state = States.Processing;

        if (webRequest == null)
        {
            state = States.Error;
            exceptionMessage = "webRequest is null.";
            HttpNetworkSystem.Instance.OnRequestFinished(this, new UnityWebRequestResponse(), exceptionAction, callback);
            yield break;
        }

        yield return webRequest.SendWebRequest();

        // 映射 UnityWebRequest.Result -> HTTPRequest.States
        switch (webRequest.result)
        {
            case UnityWebRequest.Result.Success:
                state = States.Finished;
                break;
            case UnityWebRequest.Result.ProtocolError:
                // 服务器返回了错误状态码（4xx/5xx），但响应可读，按 Finished 处理，由框架判 IsSuccess。
                state = States.Finished;
                break;
            case UnityWebRequest.Result.ConnectionError:
                if (webRequest.error != null &&
                    (webRequest.error.ToLower().Contains("timeout") || webRequest.error.ToLower().Contains("timed out")))
                {
                    state = States.TimedOut;
                }
                else
                {
                    state = States.Error;
                }
                exceptionMessage = "Request Finished with Error! " + (webRequest.error ?? "No Exception");
                break;
            case UnityWebRequest.Result.DataProcessingError:
                state = States.Error;
                exceptionMessage = "Request Finished with Error! " + (webRequest.error ?? "No Exception");
                break;
            default:
                state = webRequest.isDone ? States.Finished : States.Error;
                break;
        }

        var response = new UnityWebRequestResponse(webRequest);
        HttpNetworkSystem.Instance.OnRequestFinished(this, response, exceptionAction, callback);
    }
}
