using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private Dictionary<string, string> hostMap = new Dictionary<string, string>();

        public void Init(string apiHost) {
            this.apiHost = apiHost;
            hostMap.Clear();
        }

        /// <summary>
        /// 如果有IP缓存，就把url的host转成ip返回。
        /// 如果没有IP缓存，就直接返回原url。
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetURLByIP(string url) {
            string newUrl = url;
            Uri uri = new Uri(url);
            if (hostMap.ContainsKey(uri.Host)) {
                string ip = hostMap[uri.Host];
                var regex = new Regex(Regex.Escape(uri.Host));
                newUrl = regex.Replace(url, ip, 1);
            }
            return newUrl;
        }

        /// <summary>
        /// 将域名url转换成IP url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        public void TranslateURL(string url, Action<string, EStatus, string> callback) {
            //从url获取域名。
            Uri uri = new Uri(url);
            ParseHost(uri.Host, (string ip, EStatus status, string message) => {
                if (status == EStatus.RET_SUCCESS)
                {
                    var regex = new Regex(Regex.Escape(uri.Host));
                    string newUrl = regex.Replace(url, ip, 1);
                    callback(newUrl, status, message);
                }
                else {
                    callback(url, status, message);
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
        public void ParseHost(string domain, Action<string, EStatus, string> callback) {
            string ip = "0.0.0.0";
            string message = "ERROR";
            if (string.IsNullOrEmpty(this.apiHost)) {
                message = "Not set http-dns host yet!";
                Debug.LogWarning(message);
                callback(ip, EStatus.RET_NO_HOST, message);
                return;
            }
            //JsonObject param = new JsonObject();
            //param["domain"] = domain;
            HttpNetworkSystem.Instance.GetWebRequest(this.apiHost, "?domain="+domain, HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=> {
                if (response == null)
                {
                    message = string.Format("Request http dns failed!-{0}", response.ToString());
                    Debug.LogError(message);
                    callback(ip, EStatus.RET_ERROR_RESULT, message);
                }
                else {
                    if (!response.ContainsKey("code")) {
                        message = string.Format("Host responses wrong message format:{0}", response.ToString());
                        Debug.LogError(message);
                        callback(null, EStatus.RET_ERROR_RESULT, message);
                        return;
                    }
                    string code = response["code"].ToString();
                    if (!code.Equals("200")) {
                        message = string.Format("HTTP DNS failed!CODE:{0}", code);
                        Debug.LogError(message);
                        callback(null, EStatus.RET_ERROR_RESULT, message);
                        return;
                    }
                    if (!response.ContainsKey("ipv4")) {
                        message = string.Format("HTTP DNS server response error(not found ipv4)!");
                        Debug.LogError(message);
                        callback(null, EStatus.RET_ERROR_RESULT, message);
                        return;
                    }
                    string ip = null;
                    ip = response["ipv4"].ToString();
                    Debug.LogFormat("Response result:{0}", response.ToString());
                    hostMap[domain] = ip;
                    callback(ip, EStatus.RET_SUCCESS, message);
                }
            });
        }
    }
}

