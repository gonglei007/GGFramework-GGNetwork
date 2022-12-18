using System;

namespace GGFramework.GGNetwork {
    /// <summary>
    /// 请求响应类。
    /// 暂时只跟BestHTTP的response关联。
    /// </summary>
    public class GGHTTPResponse
    {
        public BestHTTP.HTTPResponse response;

        public GGHTTPResponse()
        {
        }

        public GGHTTPResponse(BestHTTP.HTTPResponse response)
        {
            this.response = response;
        }
    }
}
