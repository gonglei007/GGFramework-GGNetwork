using System;
using System.Collections.Generic;

namespace GGFramework.GGNetwork.HTTPDNS
{
    /// <summary>
    /// Http_DNS服务类。可以借助第三方的域名解析服务来获取一个域名的IP。
    /// TODO: 目前是针对橙域的服务来做的。后续可以进一步抽象。
    /// TODO: 在这里要缓存解析过的IP。
    /// </summary>
    public interface HTTPDNS
    {
        void QueryHost(string domain, Action<HTTPDNSSystem.Cache, HTTPDNSSystem.EStatus, string> callback);
        void QueryHosts(string[] domains, Action<List<HTTPDNSSystem.Cache>, HTTPDNSSystem.EStatus, string> callback);
    }
}

