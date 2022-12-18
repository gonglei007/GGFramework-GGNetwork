using System;
using System.Collections.Generic;
using BestHTTP;
using UnityEngine;

namespace GGFramework.GGNetwork
{
    /// <summary>
    /// 暂时不用这个类。
    /// </summary>
    public static class HTTPRequestAdapter
    {
        private static Dictionary<GGHTTPRequest, string> serviceTypeMap = new Dictionary<GGHTTPRequest, string>();

        public static void SetServiceType(this GGHTTPRequest request, string serviceType)
        {
            serviceTypeMap[request] = serviceType;
            Debug.Log(">>set service type:" + request.request.Uri.ToString());
        }

        public static string GetServiceType(this GGHTTPRequest request)
        {
            if (request == null)
            {
                throw new Exception("Illegal http request!");
            }
            Debug.Log(">>get service type:" + request.request.Uri.ToString());
            if (!serviceTypeMap.ContainsKey(request))
            {
                return null;
                //throw new Exception("http request in map lost!!!");
            }
            return serviceTypeMap[request];
        }
    }
}
