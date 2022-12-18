using System;
using SimpleJson;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// Http_DNS服务类。可以借助第三方的域名解析服务来获取一个域名的IP。
    /// TODO: 目前是针对橙域的服务来做的。后续可以进一步抽象。
    /// TODO: 在这里要缓存解析过的IP。
    /// </summary>
    public class HTTPDNS
    {
        public enum EStatus {
            RET_SUCCESS = 1000,
            RET_NO_HOST,
            RET_ERROR_RESULT,
        };

        private string apiHost = "";

        public void Init(string apiHost) {
            this.apiHost = apiHost;
        }

        /// <summary>
        /// 将域名url转换成IP url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        public void TranslateURL(string url, Action<string, EStatus> callback) {
            //从url获取域名。
            Uri uri = new Uri(url);
            ParseHost(uri.Host, (string ip, EStatus status) => {
                if (status == EStatus.RET_SUCCESS)
                {
                    string newURL = url.Replace(uri.Host, ip);
                    callback(newURL, status);
                }
                else {
                    callback(url, status);
                }
            });
        }

        /// <summary>
        /// 从远端HttpDNS请求解析，返回IP。
        /// 先从本地缓存获取，如果缓存有，就用缓存的。
        /// 如果缓存没有，就从远端api获取。
        /// 如果获取失败，就不解析了。
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="callback"></param>
        public void ParseHost(string domain, Action<string, EStatus> callback) {
            string ip = "0.0.0.0";
            if (string.IsNullOrEmpty(this.apiHost)) {
                Debug.LogWarning("Not set http-dns host yet!");
                callback(ip, EStatus.RET_NO_HOST);
                return;
            }
            //JsonObject param = new JsonObject();
            //param["domain"] = domain;
            HttpNetworkSystem.Instance.GetWebRequest(this.apiHost, "?domain="+domain, HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=> {
                if (response == null)
                {
                    //TODO
                    Debug.LogErrorFormat("Request http dns failed!-{0}", response.ToString());
                    callback(ip, EStatus.RET_ERROR_RESULT);
                }
                else {
                    if (!response.ContainsKey("ipv4")) {
                        Debug.LogErrorFormat("Host responses wrong message:{0}", response.ToString());
                        callback(null, EStatus.RET_ERROR_RESULT);
                        return;
                    }
                    string ip = null;
                    ip = response["ipv4"].ToString();
                    Debug.LogFormat("Response result:{0}", response.ToString());
                    callback(ip, EStatus.RET_SUCCESS);
                }
            });
        }
    }
}

