using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleJson;
using UnityEngine;
using System.Timers;

namespace GGFramework.GGNetwork.HTTPDNS
{
    /// <summary>
    /// Http_DNS服务类。可以借助第三方的域名解析服务来获取一个域名的IP。
    /// TODO: 目前是针对橙域的服务来做的。后续可以进一步抽象。
    /// TODO: 在这里要缓存解析过的IP。
    /// </summary>
    public class HTTPDNSSystem
    {
        public class Cache
        {
            public string domain;
            public string ip;
            public int ttl;
            public int updateTime;
        }

        public enum EStatus {
            RET_SUCCESS = 1000,
            RET_NO_ON,
            RET_NO_HOST,
            RET_ERROR_RESULT,
        };

        public bool ON
        {
            get {
                return this.on;
            }
            set {
                this.on = value;
                if (timer != null) {
                    if (this.on)
                    {
                        timer.Start();
                    }
                    else {
                        timer.Stop();
                    }
                }
            }
        }

        private HTTPDNS httpDNS = null;
        private Dictionary<string, Cache> hostMap = new Dictionary<string, Cache>();
        private bool on = false;    // 是否开启。
        private Timer timer = null;

        internal void Init(HTTPDNSFactory.Provider provider)
        {
            httpDNS = HTTPDNSFactory.CreateHTTPDNS(provider);
            hostMap.Clear();
            timer = new Timer(1000);
            timer.AutoReset = true;
            timer.Elapsed += (System.Object source, ElapsedEventArgs e) => {
                CheckAndUpdateCache();
            };
            this.ON = true;
        }

        // 定时检查并更新缓存。
        private void CheckAndUpdateCache()
        {
            foreach (string domain in hostMap.Keys) {
                Cache cache = hostMap[domain];
                int now = DateTime.Now.Second;
                if (now - cache.updateTime > cache.ttl) {
                    ParseHost(domain, (Cache cache, EStatus status, string ip) => {
                        if (status == EStatus.RET_SUCCESS) {
                            Debug.LogFormat("成功刷新域名:{0}", cache.ToString());
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 用host替换url中的host。
        /// </summary>
        /// <returns></returns>
        private string ReplaceHost(string url, string host)
        {
            string newUrl = url;
            Uri uri = new Uri(url);
            var regex = new Regex(Regex.Escape(uri.Host));
            newUrl = regex.Replace(url, host, 1);
            return newUrl;
        }

        /// <summary>
        /// 用host替换uri中的host
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private string ReplaceHost(Uri uri, string host) {
            return ReplaceHost(uri.ToString(), host);
        }

        /// <summary>
        /// 如果有IP缓存，就把url的host转成ip返回。
        /// 如果没有IP缓存，就直接返回原url。
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetURLByIP(string url) {
            if (!this.on) {
                return url;
            }
            string newUrl = url;
            Uri uri = new Uri(url);
            if (hostMap.ContainsKey(uri.Host)) {
                Cache cache = hostMap[uri.Host];
                newUrl = ReplaceHost(url, cache.ip);
            }
            return newUrl;
        }

        /// <summary>
        /// 将域名url转换成IP url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        public void TranslateURL(string url, Action<string, EStatus, string> callback) {
            if (!this.on)
            {
                callback(url, EStatus.RET_SUCCESS, "HTTP DNS is not on.");
                return;
            }
            if (httpDNS == null)
            {
                return;
            }
            //从url获取域名。
            Uri uri = new Uri(url);
            this.ParseHost(uri.Host, (Cache cache, EStatus status, string message) => {
                if (status == EStatus.RET_SUCCESS)
                { 
                    string newUrl = ReplaceHost(url, cache.ip);
                    callback(newUrl, status, message);
                }
                else {
                    callback(url, status, message);
                }
            });
        }

        public void ParseHost(string domain, Action<Cache, HTTPDNSSystem.EStatus, string> callback)
        {
            string message = "ok";
            if (!this.on)
            {
                message = "HTTP DNS is not on!";
                callback(null, HTTPDNSSystem.EStatus.RET_SUCCESS, message);
                return;
            }
            if (httpDNS == null)
            {
                message = "http-dns not set yet!";
                Debug.LogWarning(message);
                callback(null, EStatus.RET_ERROR_RESULT, message);
                return;
            }
            httpDNS.QueryHost(domain, (Cache cache, HTTPDNSSystem.EStatus status, string mesage) => {
                if (status == EStatus.RET_SUCCESS && cache != null)
                {
                    hostMap[domain] = cache;
                }
                callback(cache, status, message);
            });
        }
        public void ParseHosts(string[] domains, Action<List<Cache>, HTTPDNSSystem.EStatus, string> callback)
        {
            string message = "ok";
            if (!this.on)
            {
                message = "HTTP DNS is not on!";
                callback(null, HTTPDNSSystem.EStatus.RET_SUCCESS, message);
                return;
            }
            if (httpDNS == null)
            {
                message = "http-dns not set yet!";
                Debug.LogWarning(message);
                callback(null, EStatus.RET_ERROR_RESULT, message);
                return;
            }
            httpDNS.QueryHosts(domains, (List<Cache> cacheList, HTTPDNSSystem.EStatus status, string mesage) => {
                foreach (Cache cache in cacheList) {
                    hostMap[cache.domain] = cache;
                }
                callback(cacheList, status, message);
            });
        }
    }
}

