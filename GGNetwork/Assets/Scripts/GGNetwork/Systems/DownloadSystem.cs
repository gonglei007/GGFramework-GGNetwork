using UnityEngine;
using System;
using SimpleJson;
using System.Net;
using System.IO;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace GGFramework.GGNetwork
{
    /**
     * 文件下载管理。
     * 基于 Unity 原生 UnityWebRequest（DownloadHandlerFile 边下边写本地文件），不依赖第三方 HTTP 库。
     * TBD: GL - 是否需要支持断点续传？——暂时可以不用。
     */
    public class DownloadSystem : Singleton<DownloadSystem>
    {
        public class RequestItem
        {
            public string Url;
            public string SavePath;
            public Action<float> Progress;
            public Action<int> Total;
            public Action<string> Complete;

            /// <summary>已重试次数（内部使用，防无限重试）。</summary>
            public int RetryCount;

            /// <summary>取消标志（ClearDownLoad 置位，下载循环据此中止）。</summary>
            public bool Cancelled;

            public RequestItem(string url, string savepath, Action<float> progress, Action<int> total, Action<string> complete)
            {
                Url = url;
                SavePath = savepath;
                Progress = progress;
                Total = total;
                Complete = complete;
            }
        }

        public Action<string, string, bool, Action<bool>> onDialog = null;

        public static int FragmentSize = 1024 * 1024 * 1; //保留：兼容引用（原生下载不分片）
        public static int DOWNLOAD_SERVER_DATA_COUNT = 0;//下载服务器数据的次数
        public static int DOWNLOAD_TIMEOUT = 25000;//下载超时（毫秒）

        private Action<JsonObject> verifyFileDownloadCallback = null;
        private Action fileDownloadCallback = null;
        private JsonObject verifyFileObject = null;
        private Timer timer = new Timer(DOWNLOAD_TIMEOUT);

        public static bool IgnoreSSLErrors = false;

        /// <summary>当前正在下载的任务集合（用于取消）。</summary>
        private readonly List<RequestItem> m_ActiveDownloads = new List<RequestItem>();

        public DownloadSystem()
        {
            // 仅对 .NET 系统网络栈(如 BestHTTP/HttpWebRequest)生效；UnityWebRequest 走 Unity 原生网络栈，不受此影响。
            if (IgnoreSSLErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, ssl) => true;
            }
        }

        public void Awake()
        {
            // 确保主线程调度器已创建：UnityWebRequest 必须主线程发送。
            // HttpRequestDriver 为 MonoBehaviour，需在主线程实例化。
            HttpRequestDriver.EnsureInstance();
        }

        /// <summary>
        /// 请求下载文件到本地。
        /// 基于 UnityWebRequest.DownloadHandlerFile：边下边写盘，不占用内存。
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="downloadPath">本地保存路径</param>
        /// <param name="progress">进度回调 (0.0~1.0)</param>
        /// <param name="total">总大小回调节字</param>
        /// <param name="complete">完成回调（参数为下载完成的 Url）</param>
        public void RequestDownload(string url, string downloadPath, Action<float> progress, Action<int> total, Action<string> complete)
        {
            var item = new RequestItem(url, downloadPath, progress, total, complete);
            // 投递到主线程：添加活动记录 + 启动下载协程（线程安全；实例确保已在 Awake 创建）
            HttpRequestDriver.Enqueue(() =>
            {
                m_ActiveDownloads.Add(item);
                StartDownloadCoroutine(item);
            });
        }

        /// <summary>启动单个下载协程（在主线程执行，挂在本 MonoBehaviour 上以支持 ClearDownLoad 取消）。</summary>
        private void StartDownloadCoroutine(RequestItem item)
        {
            if (item == null || item.Cancelled) return;
            StartCoroutine(DownloadCoroutine(item));
        }

        private IEnumerator DownloadCoroutine(RequestItem item)
        {
            string url = item.Url;
            string savePath = item.SavePath;

            // 删旧文件，确保目录存在
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            UnityWebRequest.Result result = UnityWebRequest.Result.InProgress;
            float finalProgress = 0f;
            string errorMsg = "";
            long responseCode = 0;
            bool cancelled = false;

            using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                // 原生边下边写本地文件，避免大文件占用内存
                uwr.downloadHandler = new DownloadHandlerFile(savePath);
                uwr.timeout = DOWNLOAD_TIMEOUT > 0 ? DOWNLOAD_TIMEOUT / 1000 : 0;

                var op = uwr.SendWebRequest();
                float lastProgress = -1f;
                while (!op.isDone && !item.Cancelled)
                {
                    // 进度回调：仅在变化时触发，减少无谓回调
                    float p = uwr.downloadProgress;
                    if (item.Progress != null && Mathf.Abs(p - lastProgress) > 0.001f)
                    {
                        lastProgress = p;
                        item.Progress(p);
                    }
                    yield return null;
                }

                if (item.Cancelled)
                {
                    // 用户主动取消：中止请求，不做失败重试
                    cancelled = true;
                    try { uwr.Abort(); } catch (Exception) { }
                }
                else
                {
                    // 总大小（Content-Length）
                    if (item.Total != null)
                    {
                        string cl = uwr.GetResponseHeader("Content-Length");
                        if (int.TryParse(cl, out int totalSize))
                        {
                            item.Total(totalSize);
                        }
                    }

                    result = uwr.result;
                    finalProgress = uwr.downloadProgress;
                    errorMsg = uwr.error ?? "";
                    responseCode = uwr.responseCode;
                }
            } // using 块结束，释放 DownloadHandlerFile 文件句柄

            if (cancelled)
            {
                // 取消：删掉半截文件，干净结束，不回调、不重试
                try { if (File.Exists(savePath)) File.Delete(savePath); } catch (Exception) { }
                m_ActiveDownloads.Remove(item);
            }
            else if (result == UnityWebRequest.Result.Success)
            {
                if (item.Progress != null && finalProgress < 1f) item.Progress(1f);
                m_ActiveDownloads.Remove(item);
                if (item.Complete != null) item.Complete(url);
            }
            else
            {
                string status = string.IsNullOrEmpty(errorMsg)
                    ? ("Download failed! result=" + result + " (HTTP " + responseCode + ")")
                    : ("Download failed! " + errorMsg + " (HTTP " + responseCode + ")");
                OnExceptionHandler(item, status);
            }
        }

        /// <summary>
        /// 下载异常处理：删除半截文件，延迟 3 秒重试。
        /// </summary>
        private void OnExceptionHandler(RequestItem item, string message)
        {
            Debug.LogError("Download exception: " + message + "\n" + item.Url);
            try
            {
                if (File.Exists(item.SavePath))
                {
                    File.Delete(item.SavePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Download] delete file error: " + e.Message);
            }

            int retryN = item.RetryCount + 1;
            if (retryN <= MAX_RETRY)
            {
                item.RetryCount = retryN;
                StartCoroutine(RetryDelayed(item));
            }
            else
            {
                Debug.LogError("[Download] exceed max retry (" + MAX_RETRY + "), give up: " + item.Url);
                m_ActiveDownloads.Remove(item);
                if (item.Complete != null) item.Complete(null); // 经多次重试仍失败，以 null 结束
            }
        }

        private IEnumerator RetryDelayed(RequestItem item)
        {
            yield return new WaitForSeconds(3f);
            HttpRequestDriver.Enqueue(() => StartDownloadCoroutine(item));
        }

        /// <summary>
        /// 清除所有下载任务（标记取消 + 停止所有下载/重试协程）。
        /// </summary>
        public void ClearDownLoad()
        {
            // 标记所有进行中的任务为取消
            foreach (var item in m_ActiveDownloads)
            {
                if (item != null) item.Cancelled = true;
            }
            m_ActiveDownloads.Clear();
            // 停止本 MonoBehaviour 上所有协程（下载协程 + 重试等待协程）
            StopAllCoroutines();
        }

        private const int MAX_RETRY = 3;
    }
}
