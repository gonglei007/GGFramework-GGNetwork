using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using BestHTTP;
using BestHTTP.Extensions;
using SimpleJson;
using UnityEngine;

/**
 * 橙域的鹰眼网络测试类。
 * TBD: 使用Unity的Http或者DotNet原生的HTTP？
 * TBD: 为了防止网络的异常产生阻塞，在子线程(Task)里面上报http？
 */
public class EagleEye
{
    const string TASK_ID = "725373878";
    const string KEY = "AC5c9596842898e2";
    const string HOST = "http://www.asia-beta.com";
    const string GET_TEST_URI = "/edge_api/v2/get_test_url";
    const string GET_TEST_PARAM = "?task_id={0}&authts={1}&token={2}&scene=app";

    const string REPORT_URI = "/edge_api/v1/upload";
    const string REPORT_PARAM = "?task_id={0}&authts={1}&token={2}";

    const double TICKS_OF_SECONDS = 10000000;

    static long authts = 0;
    /// <summary>
    /// 同一地址的诊断间隔，避免频繁请求，单位：秒
    /// </summary>
    const long TEST_DELTA = 10;
    /// <summary>
    /// 控制网络诊断频率的字典，Key是网络地址信息，value是上一次触发的时间戳（单位秒）
    /// </summary>
    static Dictionary<string, long> TestFrequencyControlDict = new Dictionary<string, long>();
    static bool On = false;

    public class ReportParamData
    {
        public int uid;
        public string domain;
        public string server_ip;
        public int server_port;
        public string channel_id;
        public string app_id = "wj_xxsg2";

        public string protocol;
        public string reason;
        public string uri;
        public string address;
        public int statusCode;
        public string message;
        public string dataAsText;
    }

    public class ReportPostData
    {
        public int task_id;
        public string doc_id;
        public long ts;
        public string proxy_ip;
        public string create_time;
        public string platform = "probe";
        public List<URLRecord> private_urls = new List<URLRecord>();
    }

    [DataContract]
    public class URLRecord
    {
        [DataMember]
        public string download_speed;
        [DataMember]
        public double resolution_time;
        [DataMember]
        public string url;

        private bool isRecordFinish;
        [IgnoreDataMember]
        public bool IsRecordFinish
        {
            get => isRecordFinish;
            set => isRecordFinish = value;
        }
    }

    /// <summary>
    /// 调用初始化之后才打开功能。
    /// </summary>
    static public void Init() {
        EagleEye.On = true;
    }

    static public void SetProxy(string proxy)
    {
        HTTPManager.Proxy = new HTTPProxy(new Uri(proxy));
        HTTPManager.ConnectTimeout = new TimeSpan(3);
    }

    /**
     * 橙域的鹰眼网络测试类。
     * 在网络发生异常的时候触发，不要大量的反复触发。
     * @param identityInfo 每一次诊断的唯一标识，通常使用网络连接的地址
     * @param extraReportParam 额外上报的信息
     */
    static public void TestNetwork(string identityInfo, string extraReportParam = "")
    {
        if (!EagleEye.On) {
            return;
        }
        try
        {
            if (TestFrequencyControlDict.TryGetValue(identityInfo, out long lastRequestTimestamp))
            {
                long currentTimestamp = GetTimestampSeconds();
                if (lastRequestTimestamp - currentTimestamp < TEST_DELTA)
                {
                    //两次请求间隔不足10秒
                    return;
                }
                else
                {
                    TestFrequencyControlDict[identityInfo] = currentTimestamp;
                }
            }
            else
            {
                TestFrequencyControlDict.Add(identityInfo, GetTimestampSeconds());
            }

            Debug.Log($"EagleEye Start: 发起一次诊断：{identityInfo}");

            string token = GenerateToken(GET_TEST_URI);
            string completeURL = $"{HOST}{GET_TEST_URI}{string.Format(GET_TEST_PARAM, TASK_ID, authts, token)}";

            HTTPRequest request = new HTTPRequest(new Uri(completeURL), HTTPMethods.Post, (req, resp) =>
            {
                if (req.State == HTTPRequestStates.Finished)
                {
                    if (resp.IsSuccess)
                    {
                        try
                        {
                            Debug.Log($"EagleEye Success: 测试链接回复：{resp.DataAsText}");
                            JsonObject jsonObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(resp.DataAsText);
                            string code = jsonObject["code"].ToString();
                            string desc = jsonObject["desc"].ToString();

                            if (code.Equals("00"))
                            {
                                JsonObject data = jsonObject["data"] as JsonObject;

                                ReportPostData postData = new ReportPostData();
                                //把 task_id doc_id ts proxy_ip create_time 保留下来，不做任何处理，上报 API 会用到
                                postData.task_id = Convert.ToInt32(data["task_id"]);
                                postData.doc_id = data["doc_id"].ToString();
                                postData.ts = Convert.ToInt64(data["ts"]);
                                postData.proxy_ip = data["proxy_ip"].ToString();
                                postData.create_time = data["create_time"].ToString();

                                //dns_url 直接 GET 请求一下即可，不用管响应数据
                                string dns_url = data["dns_url"].ToString();
                                HTTPRequest dnsURLReq = new HTTPRequest(new Uri(dns_url), HTTPMethods.Get);
                                dnsURLReq.Send();

                                //urls 是一个数组，动态可变的，对于其中的每个 url 都需要做第 3 小节的处理，然后把结果上报。
                                JsonArray jsonArray = data["urls"] as JsonArray;
                                foreach (string url in jsonArray)
                                {
                                    URLRecord urlRecord = new URLRecord();
                                    urlRecord.url = url;
                                    postData.private_urls.Add(urlRecord);

                                    TestURL(url, postData, urlRecord);
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"EagleEye GET_TEST_URI Respone error code:{code} desc:{desc}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"EagleEye GET_TEST_URI Exception : {ex}");
                        }
                    }
                    else
                    {
                        Debug.Log($"EagleEye Error: 测试链接回复失败，url:{completeURL} resp.StatusCode:{resp.StatusCode} req.Text:{resp.DataAsText}");
                    }
                }
                else
                {
                    Debug.Log($"EagleEye Error: 请求测试链接失败，url:{completeURL} req.State:{req.State} req.Exception:{(req.Exception != null ? req.Exception.Message : string.Empty)}");
                }
            });

            Debug.Log($"EagleEye Send: 发送测试链接，url:{completeURL}");
            request.SetHeader("Content-Type", "application/json; charset=UTF-8");
            request.RawData = Encoding.UTF8.GetBytes(extraReportParam);
            request.Send();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"EagleEye TestNetwork Exception : {ex}");
        }
    }

    /**
     * 测试特定的URL。
     * 备注: 不用考虑请求的返回结果。
     * @param url: 从get_test_url中返回的urls（中的一个）。
     */
    static private void TestURL(string url, ReportPostData postData, URLRecord urlRecord) {
        //获取当前时间为 T1
        long T1 = GetTimestampTicks();
        //使用 URL 发送一次 HEAD 请求，参数为 n=1(防止缓存情况)
        string t1URL = $"{url}?n=1";
        HTTPRequest t1Request = new HTTPRequest(new Uri(t1URL), HTTPMethods.Head, (req1, resp1) => {

            //这里只管请求成功的情况，失败就中断好了
            if (req1.State == HTTPRequestStates.Finished && resp1.IsSuccess)
            {
                Debug.Log($"EagleEye 测试T1成功");
                //获取当前时间为 T2
                long T2 = GetTimestampTicks();

                //再次使用 URL 发送一次 HEAD 请求，参数为 n=2(防止缓存情况)
                string t2URL = $"{url}?n=2";
                HTTPRequest t2Request = new HTTPRequest(new Uri(t1URL), HTTPMethods.Head, (req2, resp2) =>
                {
                    //这里只管请求成功的情况，失败就中断好了
                    if (req2.State == HTTPRequestStates.Finished && resp2.IsSuccess)
                    {
                        Debug.Log($"EagleEye 测试T2成功");
                        //获取当前时间为 T3
                        long T3 = GetTimestampTicks();

                        //使用 URL 发送一次 GET 请求
                        HTTPRequest t3Request = new HTTPRequest(new Uri(url), HTTPMethods.Get, (req3, resp3) =>
                        {
                            if (req3.State == HTTPRequestStates.Finished && resp3.IsSuccess)
                            {
                                Debug.Log($"EagleEye 测试T3成功");
                                //获取当前时间为 T4
                                long T4 = GetTimestampTicks();

                                Debug.Log($"EagleEye T1:{T1}");
                                Debug.Log($"EagleEye T2:{T2}");
                                Debug.Log($"EagleEye T3:{T3}");
                                Debug.Log($"EagleEye T4:{T4}");

                                /* 域名解析时间 = （T2 - T1） - （T3 - T2）
                                 * 下载时间 = T3 - T4 - 域名解析时间
                                 * 下载速度 = 下载的字节数 / 下载时间 */

                                urlRecord.resolution_time = ((T2 - T1) - (T3 - T2)) / TICKS_OF_SECONDS;
                                double download_time = (T4 - T3) / TICKS_OF_SECONDS;
                                urlRecord.download_speed = (resp3.Data.Length / download_time).ToString("F2");
                            }
                            else
                            {
                                Debug.Log($"EagleEye Error: 测试T3 URL:{url}失败：req.State:{req3.State} req.Exception:{(req3.Exception != null ? req3.Exception.Message : string.Empty)} resp.Message：{resp3.Message}");
                                urlRecord.resolution_time = -1;
                                urlRecord.download_speed = "-1";
                            }
                            urlRecord.IsRecordFinish = true;
                            CheckToReport(postData);
                        });

                        Debug.Log($"EagleEye Send: 发送测试T3，url:{url}");
                        t3Request.Send();
                    }
                    else
                    {
                        Debug.Log($"EagleEye Error: 测试T2 URL:{t2URL}失败：req.State:{req2.State} req.Exception:{(req2.Exception != null ? req2.Exception.Message : string.Empty)} resp.Message：{resp2.Message}");
                        urlRecord.IsRecordFinish = true;
                        urlRecord.resolution_time = -1;
                        urlRecord.download_speed = "-1";
                        CheckToReport(postData);
                    }
                });
                Debug.Log($"EagleEye Send: 发送测试T2，url:{t2URL}");
                t2Request.Send();
            }
            else
            {
                Debug.Log($"EagleEye Error: 测试T1 URL:{t1URL}失败：req.State:{req1.State} req.Exception:{(req1.Exception != null ? req1.Exception.Message : string.Empty)} resp.Message：{resp1.Message}");
                urlRecord.IsRecordFinish = true;
                urlRecord.resolution_time = -1;
                urlRecord.download_speed = "-1";
                CheckToReport(postData);
            }
        });
        Debug.Log($"EagleEye Send: 发送测试T1，url:{t1URL}");
        t1Request.Send();
    }

    /**
     * 检查单次测试是否完成，如果完成就上报
     */
    static private void CheckToReport(ReportPostData postData)
    {
        foreach (URLRecord urlRecord in postData.private_urls)
        {
            if (urlRecord.IsRecordFinish == false)
            {
                return;
            }
        }

        //所有url都测试完成，可以上报
        string postString = SimpleJson.SimpleJson.SerializeObject(postData); 

        string token = GenerateToken(REPORT_URI);
        string completeURL = $"{HOST}{REPORT_URI}{string.Format(REPORT_PARAM, TASK_ID, authts, token)}";

        HTTPRequest uploadReq = new HTTPRequest(new Uri(completeURL), HTTPMethods.Post, (req, resp) =>
        {
            if (req.State == HTTPRequestStates.Finished)
            {
                if (resp.IsSuccess)
                {
                    Debug.Log($"EagleEye Success: 上报成功 task_id:{postData.task_id}");
                }
                else
                {
                    Debug.Log($"EagleEye Error: 上报链接回复失败，url:{completeURL} resp.StatusCode:{resp.StatusCode} req.Text:{resp.DataAsText}");
                }
            }
            else
            {
                Debug.Log($"EagleEye Error: 请求上报链接失败，url:{completeURL} req.State:{req.State} req.Exception:{(req.Exception != null ? req.Exception.Message : string.Empty)}");
            }
        });

        Debug.Log($"EagleEye Send: 发送上报链接，url:{completeURL} post:{postString}");
        uploadReq.SetHeader("Content-Type", "application/json; charset=UTF-8");
        uploadReq.RawData = Encoding.UTF8.GetBytes(postString);
        uploadReq.Send();
    }

    /**
     * 生成橙域的Token。用于上面的请求的token生成。
     * 算法：md5(KEY + uri + task_id + ts)
     * TBD: ts，要跟橙域确认一下，哪些步骤共用一个ts。
     */
    static private string GenerateToken(string uri) {
        
        authts = GetTimestampSeconds();
        string token = $"{KEY}{uri}{TASK_ID}{authts}".CalculateMD5Hash();
        return token;
    }

    /// <summary>
    /// 获取秒级时间戳
    /// </summary>
    /// <returns></returns>
    static private long GetTimestampSeconds()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        long timestamp = Convert.ToInt64(ts.TotalSeconds);
        return timestamp;
    }

    /// <summary>
    /// 获取Tick级时间戳
    /// </summary>
    /// <returns></returns>
    static private long GetTimestampTicks()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        long timestamp = Convert.ToInt64(ts.Ticks);
        return timestamp;
    }
}

