using System;
using GGFramework.GGNetwork;

/// <summary>
/// 基于 UnityWebRequest 的 HTTP 底层工厂（实现 IHTTPFactory）。
/// 对应 BestHTTPFactory，作为 Unity 原生 HTTP 实现接入框架。
/// </summary>
internal class UnityWebRequestFactory : IHTTPFactory
{
    public HTTPRequest CreateHTTPRequest(Uri uri)
    {
        return new UnityWebRequestRequest(uri);
    }

    public HTTPResponse CreateHTTPResponse()
    {
        return new UnityWebRequestResponse();
    }

    public HTTPForm CreateHTTPForm()
    {
        // 基类 HTTPForm 内部持有 WWWForm，UnityWebRequest 直接可用。
        return new HTTPForm();
    }
}
