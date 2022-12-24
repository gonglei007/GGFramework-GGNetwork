using System;

namespace GGFramework.GGNetwork {
    /// <summary>
    /// 请求响应类。
    /// 暂时只跟BestHTTP的response关联。
    /// </summary>
    public class HTTPResponse
    {
        public HTTPResponse()
        {
            //this.response = new BestHTTP.HTTPResponse();
        }

        public virtual bool IsSuccess()
        {
            return false;
        }

        public virtual int GetStatusCode()
        {
            return 0;
        }

        public virtual string GetData()
        {
            return null;
        }

        public virtual string GetMessage()
        {
            return null;
        }
    }
}
