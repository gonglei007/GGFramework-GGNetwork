using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GGFramework.GGNetwork.HTTPDNS
{
    internal class HTTPDNSFactory
    {
        public enum Provider
        {
            CY,         // 橙域
            Tencent,    // 腾讯云
            Ali,        // 阿里云
        }
        public static HTTPDNS CreateHTTPDNS(Provider provider)
        {
            HTTPDNS httpDNS = null;
            switch (provider)
            {
                case Provider.CY:
                    httpDNS = new CyHTTPDNS();
                    break;
                default:
                    Debug.LogError("This HTTPDNS provider not implemented!");
                    break;
            }
            return httpDNS;
        }
    }
}
