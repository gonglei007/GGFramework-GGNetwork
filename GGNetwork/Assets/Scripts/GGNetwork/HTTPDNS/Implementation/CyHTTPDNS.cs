using System;
using SimpleJson;
using UnityEngine;
using System.Collections.Generic;

namespace GGFramework.GGNetwork.HTTPDNS
{
    internal class CyHTTPDNS: HTTPDNS
    {
        //public const string HTTP_DNS_HOST = "103.150.251.71";   // 正式
        public const string HTTP_DNS_HOST = "119.8.61.184:40021";   // 测试
        public const string HTTP_DNS_API_QUERY = "http://" + HTTP_DNS_HOST + "/v1/dns/query";
        public const string HTTP_DNS_API_MULTI_QUERY = "http://" + HTTP_DNS_HOST + "/v1/dns/query_multi";
        private const int HTTP_TIMEOUT = 10;

        private System.Random random = new System.Random();

        private string PickOneIP(JsonArray ipList)
        {
            string ip = null;
            if (ipList != null && ipList.Count > 0)
            {
                int randIndex = random.Next(ipList.Count);
                ip = ipList[randIndex].ToString();
            }
            return ip;
        }

        /// <summary>
        /// 从远端HttpDNS请求解析，返回IP。
        /// 先从本地缓存获取，如果缓存有，就用缓存的。
        /// 如果缓存没有，就从远端api获取。
        /// 如果获取失败，就不解析了。
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="callback"></param>
        public void QueryHost(string domain, Action<HTTPDNSSystem.Cache, HTTPDNSSystem.EStatus, string> callback)
        {
            HTTPDNSSystem.Cache cache = null;
            string message = "OK";
            //if (string.IsNullOrEmpty(HTTP_DNS_API_QUERY))
            //{
            //    message = "Not set http-dns host yet!";
            //    Debug.LogWarning(message);
            //    callback(ip, HTTPDNSSystem.EStatus.RET_NO_HOST, message);
            //    return;
            //}
            //JsonObject param = new JsonObject();
            //param["domain"] = domain;
            HttpNetworkSystem.Instance.GetWebRequest(HTTP_DNS_API_QUERY, "?domain=" + domain, HttpNetworkSystem.ExceptionAction.Ignore, (JsonObject response) => {
                if (response == null)
                {
                    message = string.Format("Request http dns failed!-{0}", response.ToString());
                    Debug.LogError(message);
                    callback(cache, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                }
                else
                {
                    Debug.LogFormat("Response result:{0}", response.ToString());
                    //if (!response.ContainsKey("code"))
                    //{
                    //    message = string.Format("Host responses wrong message format:{0}", response.ToString());
                    //    Debug.LogError(message);
                    //    callback(null, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                    //    return;
                    //}
                    //string code = response["code"].ToString();
                    //if (!code.Equals("200"))
                    //{
                    //    message = string.Format("HTTP DNS failed!CODE:{0}", code);
                    //    Debug.LogError(message);
                    //    callback(null, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                    //    return;
                    //}
                    if (!response.ContainsKey("ipv4"))
                    {
                        message = string.Format("HTTP DNS server response error(not found ipv4)!");
                        Debug.LogError(message);
                        callback(null, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                        return;
                    }
                    JsonArray ipList = response["ipv4"] as JsonArray;
                    string ip = PickOneIP(ipList);
                    if (ip != null) {
                        cache = new HTTPDNSSystem.Cache();
                        cache.domain = domain;
                        cache.ip = ip;
                        cache.ttl = Convert.ToInt32(response["ttl"].ToString());
                        cache.ResetUpdateTime();
                    }
                    callback(cache, HTTPDNSSystem.EStatus.RET_SUCCESS, message);
                }
            });
        }
        /// <summary>
        /// 从远端HttpDNS请求解析，返回IP。
        /// 先从本地缓存获取，如果缓存有，就用缓存的。
        /// 如果缓存没有，就从远端api获取。
        /// 如果获取失败，就不解析了。
        /// </summary>
        /// <param name="domains"></param>
        /// <param name="callback"></param>
        public void QueryHosts(string[] domains, Action<List<HTTPDNSSystem.Cache>, HTTPDNSSystem.EStatus, string> callback)
        {
            List<HTTPDNSSystem.Cache> dnsList = new List<HTTPDNSSystem.Cache>();
            string message = "ERROR";
            //if (string.IsNullOrEmpty(this.apiHost))
            //{
            //    message = "Not set http-dns host yet!";
            //    Debug.LogWarning(message);
            //    callback(ips, EStatus.RET_NO_HOST, message);
            //    return;
            //}
            //JsonObject param = new JsonObject();
            //param["domain"] = domain;
            // Temp set timeout to short time.
            string domainParam = String.Join(",", domains);
            int preConnectTimeout = ServiceCenter.HttpConnectTimeout;
            int preRequestTimeout = ServiceCenter.HttpRequestTimeout;
            ServiceCenter.HttpConnectTimeout = HTTP_TIMEOUT;
            ServiceCenter.HttpRequestTimeout = HTTP_TIMEOUT;
            HttpNetworkSystem.Instance.GetWebRequest(HTTP_DNS_API_MULTI_QUERY, "?domain=" + domainParam, HttpNetworkSystem.ExceptionAction.Ignore, (JsonObject response) => {
                ServiceCenter.HttpConnectTimeout = preConnectTimeout;
                ServiceCenter.HttpRequestTimeout = preRequestTimeout;
                if (response == null)
                {
                    message = string.Format("Request http dns failed!-{0}", response.ToString());
                    Debug.LogError(message);
                    callback(dnsList, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                }
                else
                {
                    //if (!response.ContainsKey("code"))
                    //{
                    //    message = string.Format("Host responses wrong message format:{0}", response.ToString());
                    //    Debug.LogError(message);
                    //    callback(dnsList, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                    //    return;
                    //}
                    //string code = response["code"].ToString();
                    //if (!code.Equals("200"))
                    //{
                    //    message = string.Format("HTTP DNS failed!CODE:{0}", code);
                    //    Debug.LogError(message);
                    //    callback(dnsList, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                    //    return;
                    //}
                    if (!response.ContainsKey("dns_list"))
                    {
                        message = string.Format("HTTP DNS server response error(not found dns_list)!");
                        Debug.LogError(message);
                        callback(dnsList, HTTPDNSSystem.EStatus.RET_ERROR_RESULT, message);
                        return;
                    }
                    JsonArray dnsArray = response["dns_list"] as JsonArray;
                    foreach(JsonObject dnsItem in dnsArray) {
                        JsonArray ipList = dnsItem["ips"] as JsonArray;
                        string ip = PickOneIP(ipList);
                        if (ip != null) {
                            HTTPDNSSystem.Cache cache = new HTTPDNSSystem.Cache();
                            cache.domain = dnsItem["domain"].ToString();
                            cache.ip = ip;
                            cache.ttl = Convert.ToInt32(dnsItem["ttl"].ToString());
                            cache.ResetUpdateTime();
                            dnsList.Add(cache);
                        }
                    }
                    Debug.LogFormat("Response result:{0}", response.ToString());
                    callback(dnsList, HTTPDNSSystem.EStatus.RET_SUCCESS, message);
                }
            });
        }
    }
}
