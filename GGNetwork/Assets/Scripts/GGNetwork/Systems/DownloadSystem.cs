using UnityEngine;
using System;
using SimpleJson;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
//using CI.HttpClient;
using BestHTTP;

namespace GGFramework.GGNetwork
{
    /**
     * 文件下载管理。
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

            public RequestItem(string url, string savepath, Action<float> progress, Action<int> total, Action<string> complete)
            {
                Url = url;
                SavePath = savepath;
                Progress = progress;
                Total = total;
                Complete = complete;
            }

        }

        class DownloadItem
        {
            public string url;
            public string filePath;
            public Action<Exception> callback;
            public DownloadItem(string url, string filePath, Action<Exception> callback)
            {
                this.url = url;
                this.filePath = filePath;
                this.callback = callback;
            }
        }
        public Action<string, string, bool, Action<bool>> onDialog = null;

        public static int FragmentSize = 1024 * 1024 * 1; //HTTPResponse.MinBufferSize;
        public static int DOWNLOAD_SERVER_DATA_COUNT = 0;//下载服务器数据的次数
        public static int DOWNLOAD_TIMEOUT = 25000;//下载超时（毫秒）

        private Action<JsonObject> verifyFileDownloadCallback = null;
        private Action fileDownloadCallback = null;
        private JsonObject verifyFileObject = null;
        private Timer timer = new Timer(DOWNLOAD_TIMEOUT);
        private struct CallbackItem
        {
            Action<Exception> callback;
            Exception e;
            public CallbackItem(Action<Exception> callback, Exception e)
            {
                this.callback = callback;
                this.e = e;
            }

            public void DoCallback()
            {
                this.callback(e);
            }
        }
        private Queue<CallbackItem> callbackQueue = new Queue<CallbackItem>();

        private Queue<RequestItem> m_DownLoads = new Queue<RequestItem>();

        public DownloadSystem()
        {
            ServicePointManager.ServerCertificateValidationCallback = (s, cert, chain, ssl) => true;
        }

        public void Awake()
        {
            HTTPManager.MaxConnectionPerServer = 5;    // 支持20个并行下载。
        }

        public void Update()
        {
            //if (m_DownLoads.Count > 0)
            //{
            //    if (!DownlaodFile._Instance.IsDownLoadLimit)
            //    {
            //        RequestItem item = m_DownLoads.Dequeue();
            //        DownlaodFile._Instance.StartDownLoad(item.Url, item.SavePath, item.Progress, item.Total, item.Complete);
            //    }
            //}
            if (callbackQueue.Count > 0)
            {
                callbackQueue.Dequeue().DoCallback();
            }
        }

        //public void DoDownload(HTTPRequest request) {
        //}

        public BestHTTP.HTTPRequest RequestDownload(string url, string downloadPath, Action<float> progress, Action<int> total, Action<string> complete)
        {
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }
            string targetFilePath = Path.GetDirectoryName(downloadPath);
            if (!Directory.Exists(targetFilePath))
            {
                Directory.CreateDirectory(targetFilePath);
            }

            BestHTTP.HTTPRequest request = new BestHTTP.HTTPRequest(new Uri(url), (req, resp) =>
            {
                OnRequestFinished(req, resp, downloadPath, complete);

            });
#if !BESTHTTP_DISABLE_CACHING && (!UNITY_WEBGL || UNITY_EDITOR)
            // If we are writing our own file set it true(disable), so don't duplicate it on the file-system
            request.DisableCache = true;
#endif
            // We want to access the downloaded bytes while we are still downloading
            request.UseStreaming = true;
            // Set a reasonable high fragment size. Here it is 5 megabytes.
            request.StreamFragmentSize = FragmentSize;
            // Start Processing the request
            request.Send();
            return request;
        }

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="item"></param>
        //public DownloadSystem.RequestItem RequestDownload(string url, string downloadPath, Action<float> progress, Action<int> total, Action<string> complete)
        //{
        //    DownloadSystem.RequestItem item = new DownloadSystem.RequestItem(url, downloadPath, progress, total, complete);
        //    m_DownLoads.Enqueue(item);
        //    return item;
        //}

        /// <summary>
        /// 清除所有下载任务
        /// </summary>
        public void ClearDownLoad()
        {
            //DownlaodFile._Instance.ClearData();
        }

        private IEnumerator DelayDownload(BestHTTP.HTTPRequest request, float delay)
        {
            yield return new WaitForSeconds(delay);
            HTTPManager.SendRequest(request);
        }

        private int WriteFile(FileStream fs, List<byte[]> fragments)
        {
            int length = 0;
            if (fragments != null && fragments.Count > 0)
            {
                for (int i = 0; i < fragments.Count; ++i)
                {
                    // Save how many bytes we wrote successfully
                    length += fragments[i].Length;
                    fs.Write(fragments[i], 0, fragments[i].Length);
                }
            }
            return length;
        }

        public void OnRequestFinished(BestHTTP.HTTPRequest originalRequest, BestHTTP.HTTPResponse response, string downloadPath, Action<string> callback)
        {
            FileStream fs = originalRequest.Tag as System.IO.FileStream;
            string status = "";
            switch (originalRequest.State)
            {
                // The request is currently processed. With UseStreaming == true, we can get the streamed fragments here
                case HTTPRequestStates.Processing:
                    status = "Processing";
                    // Get the fragments, and save them
                    try
                    {
                        if (fs == null)
                        {
                            originalRequest.Tag = fs = new System.IO.FileStream(downloadPath, System.IO.FileMode.Create);
                        }
                        WriteFile(fs, response.GetStreamedFragments());
                    }
                    catch (Exception e)
                    {
                        status = "Download failed!";
                        OnExceptionHandler(originalRequest, downloadPath, status);
                    }
                    break;

                // The request finished without any problem.
                case HTTPRequestStates.Finished:
                    if (response.IsSuccess)
                    {
                        // Completely finished
                        if (response.IsStreamingFinished)
                        {
                            status = "Streaming finished!";
                            try
                            {
                                if (fs == null)
                                {
                                    originalRequest.Tag = fs = new System.IO.FileStream(downloadPath, System.IO.FileMode.Create);
                                }
                                WriteFile(fs, response.GetStreamedFragments());
                                fs.Close();
                            }
                            catch (Exception e)
                            {
                                status = "Download failed!";
                                OnExceptionHandler(originalRequest, downloadPath, status);
                            }
                            if (callback != null)
                            {
                                callback(originalRequest.Uri.ToString());
                            }
                        }
                        else
                        {
                            status = "Processing";
                        }
                    }
                    else
                    {
                        status = string.Format("Request finished Successfully, but the server sent an error. Status Code: {0}-{1} Message: {2}",
                                                        response.StatusCode,
                                                        response.Message,
                                                        response.DataAsText);
                        OnExceptionHandler(originalRequest, downloadPath, status);
                    }
                    break;

                // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                case HTTPRequestStates.Error:
                    status = "Request Finished with Error! " + (originalRequest.Exception != null ? (originalRequest.Exception.Message + "\n" + originalRequest.Exception.StackTrace) : "No Exception");
                    OnExceptionHandler(originalRequest, downloadPath, status);
                    break;

                // The request aborted, initiated by the user.
                case HTTPRequestStates.Aborted:
                    status = "Request Aborted!";
                    Debug.LogWarning(status);
                    break;

                // Connecting to the server is timed out.
                case HTTPRequestStates.ConnectionTimedOut:
                    status = "Connection Timed Out!";
                    OnExceptionHandler(originalRequest, downloadPath, status);
                    break;

                // The request didn't finished in the given time.
                case HTTPRequestStates.TimedOut:
                    status = "Processing the request Timed Out!";
                    OnExceptionHandler(originalRequest, downloadPath, status);
                    break;
            }
            if (originalRequest.State != HTTPRequestStates.Processing)
            {
                if (fs != null)
                {
                    fs.Dispose();
                }
                originalRequest.Tag = null;
            }
        }

        private void OnExceptionHandler(BestHTTP.HTTPRequest originalRequest, string downloadPath, string message)
        {
            Debug.LogError("Download exception:" + message + "\n" + originalRequest.Uri.ToString());
            //Debug.LogWarning("thread id-"+System.Threading.Thread.CurrentThread.ManagedThreadId);
            System.IO.File.Delete(downloadPath);
            CoroutineUtil.DoCoroutine(DelayDownload(originalRequest, 3.0f));

            //if (exceptionAction == ExceptionAction.AutoRetry || exceptionAction == ExceptionAction.ConfirmRetry)
            //{
            //    string serviceType = originalRequest.GetServiceType();
            //    RefereshServiceHost(serviceType);
            //}
            //if (onDialog != null)
            //{
            //    onDialog(TextSystem.GetText("download-error"), message, true, (bool retry) => {
            //        HTTPManager.SendRequest(originalRequest);
            //    });
            //}
        }
    }

}
