using UnityEngine.Networking;
using GGFramework.GGNetwork;

/// <summary>
/// 基于 UnityWebRequest 的响应封装。
/// 对应 BestHTTPResponse，包装 UnityWebRequest 完成后的结果。
/// </summary>
internal class UnityWebRequestResponse : HTTPResponse
{
    private UnityEngine.Networking.UnityWebRequest request;

    public UnityWebRequestResponse() { }

    public UnityWebRequestResponse(UnityEngine.Networking.UnityWebRequest request)
    {
        this.request = request;
    }

    public override bool IsSuccess()
    {
        if (request == null) return false;
        // 网络层成功 = request 正常完成（HTTP 2xx）。
        return request.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
    }

    public override int GetStatusCode()
    {
        return (int)request.responseCode;
    }

    public override string GetData()
    {
        if (request == null || request.downloadHandler == null) return null;
        return request.downloadHandler.text;
    }

    public override string GetMessage()
    {
        if (request == null) return null;
        return string.IsNullOrEmpty(request.error) ? request.result.ToString() : request.error;
    }
}
