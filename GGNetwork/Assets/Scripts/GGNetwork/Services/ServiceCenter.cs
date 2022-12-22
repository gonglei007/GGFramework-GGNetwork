using System;
using SimpleJson;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// （游戏）网络服务中心类。
    /// 获取跟游戏的网络（而非功能）相关的信息的接口。
    /// 网络相关的测试接口。
    /// </summary>
    public class ServiceCenter : Singleton<ServiceCenter>
    {
        //TODO: 后面可以做到配置中，从Init传进来。
        public const string HTTP_DNS_HOST = "http://103.150.251.71/v1/dns/query";
        //TODO: GL - 从服务端获取、更新此参数
        public static int HttpConnectTimeout = 8;
        public static int HttpRequestTimeout = 15;

        private string serviceCenterUrl = null;     // 服务中心的地址。
        private HTTPDNS httpDNS = new HTTPDNS();
        public HTTPDNS HTTPDNS {
            get {
                return httpDNS;
            }
        }

        public void Init(string serviceCenterUrl = null) {
            this.serviceCenterUrl = serviceCenterUrl;
            // 这个项目先不开启下面两个服务。等需要的时候再开启。
            //httpDNS.Init(HTTP_DNS_HOST);
            //EagleEye.Init();
        }

        /// <summary>
        /// 刷新指定类型服务的host。
        /// </summary>
        /// <param name="type"></param>
        public void RefereshServiceHost(string type, Action<string, string> callback)
        {
            if (type == null || string.IsNullOrEmpty(this.serviceCenterUrl))
            {
                return;
            }
            JsonObject paramObject = new JsonObject();
            paramObject["type"] = type;
            HttpNetworkSystem.Instance.PostWebRequest(this.serviceCenterUrl, "getService", paramObject, HttpNetworkSystem.ExceptionAction.Silence, (JsonObject response) => {
                try
                {
                    int code = Convert.ToInt32(response["code"]);

                    if (NetworkConst.CODE_OK == code)
                    {
                        string host = response["address"].ToString();
                        callback(type, host);
                    }
                }
                catch (Exception e)
                {
                    GameDebugger.sPushLog("Request getServiceList failed!!!" + e.ToString());
                }
            });
        }
    }
}
