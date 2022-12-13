#if UNITY_2017_1_OR_NEWER
#define DotNet40
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

//下载资源文件，支持断点续传
public class DownlaodFile : MonoBehaviour {

    private static DownlaodFile instance;
    public static DownlaodFile _Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new GameObject("Download").AddComponent<DownlaodFile>();
            }
            return instance;
        }
    }

    public class RequestInfo
    {
        public string SavePath;
        public UnityWebRequest WebRequest;
        public string Url;
        public Action<float> Progress;
        public Action<int> TotalLength;
        public Action<string> Complete;

        public RequestInfo(string savePath, UnityWebRequest request,string url, Action<float> progress, Action<int> totalLength, Action<string> complete)
        {
            SavePath = savePath;
            WebRequest = request;
            Url = url;
            Progress = progress;
            TotalLength = totalLength;
            Complete = complete;
        }
    }

    /// <summary>
    /// 所有需要下载的资源队列
    /// </summary>
    public List<RequestInfo> listRequest = new List<RequestInfo>();    //下载请求的列表

    /// <summary>
    /// 下载完成后需要清除的Url列表
    /// </summary>
    List<RequestInfo> removeList = new List<RequestInfo>();

    /// <summary>
    /// 下载单个文件的时间
    /// </summary>
    private float DownloadTime = 0;

    private bool IsStartDownLoad = false;

    public void ClearData()
    {
        foreach (RequestInfo requestInfo in listRequest)
        {
            RequestDispose(requestInfo, true);
        }
        listRequest.Clear();
        removeList.Clear();
        DownloadTime = 0;
        IsStartDownLoad = false;
    }
    private bool IsInlistRequest(string url)
    {
        for (int i = 0; i < listRequest.Count; i++)
        {
            if (listRequest[i].Url == url)
                return true;
        }

        return false;
    }

    public bool IsDownLoadLimit
    {
        get
        {
            return listRequest.Count >= 20;
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="url"></param>
    /// <param name="savePath"></param>
    /// <param name="progress"></param>
    /// <param name="TotalLength"></param>
    /// <param name="complete"></param>
    public  void StartDownLoad(string url,string savePath, Action<float> progress, Action<int> TotalLength, Action<string> complete)
    {
        if (IsDownLoadLimit)
            return;

        IsStartDownLoad = true;

        if (IsInlistRequest(url))
        {
            Debug.Log("下载列表已经存在路径=>" + url);
            return;
        }

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        string targetFilePath = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(targetFilePath))
            Directory.CreateDirectory(targetFilePath);

        RequestInfo requestInfo = new RequestInfo(savePath, null, url, progress, TotalLength, complete);

        Send(requestInfo);

        listRequest.Add(requestInfo);  
    }

    private void Send(RequestInfo requestInfo)
    {
        _DownloadHandler loadHandler = new _DownloadHandler(requestInfo.SavePath);
        loadHandler.RegisteProgressBack(requestInfo.Progress);
        loadHandler.RegisteReceiveTotalLengthBack(requestInfo.TotalLength);
        loadHandler.RegisteCompleteBack(requestInfo.Complete);
        UnityWebRequest request = UnityWebRequest.Get(requestInfo.Url);
        request.timeout = 25;
        request.chunkedTransfer = true;
        request.disposeDownloadHandlerOnDispose = true;
        request.SetRequestHeader("Range", "bytes=" + loadHandler.DownedLength + "-");
        request.downloadHandler = loadHandler;
        request.Send();
        requestInfo.WebRequest = request;
    }

    private void Update()
    {
        if (!IsStartDownLoad)
            return;

        if (DownloadTime < 30)
        {
            DownloadTime += Time.deltaTime;
        }
        else
        {
            //TODO:GL - 使用新的UI系统！
            /*
            GameLauncher.ShowDialog
               (
                       TextManager.GetText("network_error"),
                       null,
                       ConfirmStyle.YesNo, XiaoChanStyle.normal,
                       (bool ret) =>
                       {
                           if (ret)
                               DownloadTime = 0;
                           else
                               Application.Quit();
                       },
                       null,
                       TextManager.GetText("retry"),
                       TextManager.GetText("quit"),
                       false
              );
              */
        }

        for (int i = 0; i < listRequest.Count; i++)
        {
            UnityWebRequest request = listRequest[i].WebRequest;
#if DotNet40
            bool result = request.isNetworkError;
#else
            bool result = request.isError;
#endif

            if (request!= null && result)
            {
                if (GameDebugger.Instance != null)
                    GameDebugger.Instance.PushLog(string.Format("下载出错:{0},准备重试下载", request.error));
                RequestDispose(listRequest[i], false);
                Send(listRequest[i]);
                continue;
            }
            if (request!=null && request.isDone)
            {
                removeList.Add(listRequest[i]);
                RequestDispose(listRequest[i], true);
                DownloadTime = 0;
            }
        }

        for (int i = 0; i < removeList.Count; i++)
        {
            if(listRequest.Contains(removeList[i]))
                listRequest.Remove(removeList[i]);
        }
        removeList.Clear();
    }

    void OnApplicationQuit()
    {
        ClearData();
    }

    /// <summary>
    /// 释放下载请求
    /// </summary>
    /// <param name="requestInfo"></param>
    private void RequestDispose(RequestInfo requestInfo,bool isClearSelf)
    {
        if (requestInfo == null || requestInfo.WebRequest == null)
            return;

        (requestInfo.WebRequest.downloadHandler as _DownloadHandler).OnDispose();  //释放资源
        requestInfo.WebRequest.Dispose();
        requestInfo.WebRequest = null;

        if (isClearSelf)
        {
            requestInfo.Progress = null;
            requestInfo.Complete = null;
            requestInfo.TotalLength = null;
            requestInfo.Url = null;
            requestInfo.SavePath = null;
            requestInfo = null;
        }
    }
}
