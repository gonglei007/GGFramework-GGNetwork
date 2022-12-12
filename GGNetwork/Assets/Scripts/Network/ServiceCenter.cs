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
        private string serviceCenterUrl = null;     // 服务中心的地址。

        public void Init(string serviceCenterUrl = null) {
            this.serviceCenterUrl = serviceCenterUrl;
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
