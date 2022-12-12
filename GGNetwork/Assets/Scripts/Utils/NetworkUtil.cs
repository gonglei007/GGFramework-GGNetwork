using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using UnityEngine;
using SimpleJson;

public class NetworkUtil : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// 检查是否为4G网络。编辑器环境设定为4G，便于调试。
    /// </summary>
    /// <returns></returns>
    public static bool Is4GNetwork() {
        bool is4G = false;
        if (!Application.isEditor && Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            is4G = true;
        }
#if UNITY_EDITOR
        //編輯器模式始終設定爲4G，即始終彈提示。
        is4G = true;
#endif
        return is4G;
    }

    /// <summary>
    /// 将文件长度转化为可读的字符串，例如 1024Byte = 1K
    /// </summary>
    /// <param name="fileLength"></param>
    /// <returns></returns>
    public static string GetFileLengthString(double fileLength) {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (fileLength >= 1024 && order < sizes.Length - 1)
        {
            order++;
            fileLength = fileLength / 1024;
        }
        string result = string.Format("{0:0.##} {1}", fileLength, sizes[order]);
        return result;
    }

    public static string GetIPFromHost(string host) {
        string ip = "0.0.0.0";
        try
        {
            IPAddress[] IPHost = Dns.GetHostAddresses(host);
            //you might get more than one ip for a hostname since 
            //DNS supports more than one record

            if (IPHost.Length > 0)
            {
                ip = IPHost[0].ToString();
                return ip;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("{0}\n{1}\n{2}", e.ToString(), host, ip));
        }
        finally {
        }
        return ip;
    }

    public static string GetHostFromUrl(string url) {
        Uri uri = new Uri(url);
        string host = uri.Host;
        return host;
    }

    public static string Sign(JsonObject messageObject, string secretKey) {
        if (messageObject == null || messageObject.Keys.Count <= 0) {
            return null;
        }
        string[] keyList = new string[messageObject.Keys.Count];
        messageObject.Keys.CopyTo(keyList, 0);
        Array.Sort(keyList, StringComparer.InvariantCulture);
        string signText = "";
        for (int i = 0; i < keyList.Length; ++i) {
            string key = keyList[i];
            object value = messageObject[key];
            signText += value.ToString() + "#";
            //if (value.GetType() == typeof(JsonObject) || value.GetType() == typeof(JsonArray)) {
            //}
        }
        signText += secretKey;
        return encodeSign(signText);
    }

    public static string Sign(string message, string secretKey) {
        string[] param1 = message.Split('?');
        if (param1.Length < 2) {
            throw new Exception("Illegal params!!!"+message);
        }
        string[] param2 = param1[1].Split('&');
        Dictionary<string, string> paramDict = new Dictionary<string, string>();
        foreach (string param2Item in param2) {
            string[] param3 = param2Item.Split('=');
            if (param3.Length == 2) {
                paramDict[param3[0]] = param3[1];
            }
        }
        string[] keyList = new string[paramDict.Keys.Count];
        paramDict.Keys.CopyTo(keyList, 0);
        Array.Sort(keyList, StringComparer.InvariantCulture);
        string signText = "";
        for (int i = 0; i < keyList.Length; ++i)
        {
            string key = keyList[i];
            object value = paramDict[key];
            signText += value + "#";
        }
        signText += secretKey;
        return encodeSign(signText);
    }

    private static string encodeSign(string signText) {
        MD5 md5 = MD5.Create();
        byte[] byteArray = Encoding.UTF8.GetBytes(signText);
        byte[] hashArray = md5.ComputeHash(byteArray);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashArray.Length; i++)
        {
            sb.Append(hashArray[i].ToString("X2"));
        }
        return sb.ToString().ToLower();
    }

    public static long GetTimeStamp() {
#if UNITY_2017_1_OR_NEWER
        long timeStamp = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() * 1000;
#else
        long timeStamp = new DateTimeOffset(DateTime.Now).Second * 1000;
#endif
        return timeStamp;
    }
}
