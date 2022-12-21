using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
//using GOLib.Tool;

//下载句柄，自定义部分操作和数据
public class _DownloadHandler : DownloadHandlerScript
{
    //文件流，用来写入文件数据
    private FileStream fs;  

    //要下载的文件总长度
    private int _contentLength = 0;
    public int ContentLength
    {
        get { return _contentLength; }
    }

    //已经下载的数据长度
    private int _downedLength = 0;   
    public int DownedLength
    {
        get { return _downedLength; }
    }

    //要保存的文件名称，带扩展名
    private string _fileName;    
    public string FileName
    {
        get { return _fileName; }
    }

    //下载中的临时文件名，可自定义：fileName+.temp
    public string FileNameTemp
    {
        get { return _fileName + ".temp"; }
    }

    //要保存的文件路径
    private string savePath = null;
    //保存的文件目录名称
    public string DirectoryPath
    {
        get { return savePath.Substring(0, savePath.LastIndexOf('/')); }
    }

    #region 消息通知事件

    private event Action<int> _eventTotalLength = null; //接收到数据总长度的事件回调，传递的参数是文件总大小，单位：字节

    private event Action<float> _eventProgress = null;    //进度通知事件，传递的是进度浮点数

    private event Action<string> _eventComplete = null;  //完成后的事件回调，传递的参数是文件路径

    #endregion

    #region 注册消息事件

    //注册收到文件总长度的事件，传递的参数是文件总大小，单位：字节
    public void RegisteReceiveTotalLengthBack(Action<int> back)
    {
        if (back != null)
            _eventTotalLength = back;
    }

    //注册进度事件，传递的是进度浮点数
    public void RegisteProgressBack(Action<float> back)
    {
        if (back != null)
            _eventProgress = back;
    }

    //注册下载完成后的事件，传递的参数是文件路径
    public void RegisteCompleteBack(Action<string> back)
    {
        if (back != null)
            _eventComplete = back;
    }

    #endregion

    /// <summary>
    /// 初始化下载句柄，定义每次下载的数据上限为200kb
    /// </summary>
    /// <param name="filePath">保存到本地的文件路径</param>
    public _DownloadHandler(string filePath):base(new byte[1024*200])
    {
        savePath = filePath.Replace('\\','/');
        _fileName = savePath.Substring(savePath.LastIndexOf('/') + 1);  //获取文件名

         this.fs = new FileStream(savePath + ".temp", FileMode.Append, FileAccess.Write);    //文件流操作的是临时文件，结尾添加.temp扩展名
         _downedLength = (int)fs.Length;  //设置已经下载的数据长度
    }

    /// <summary>
    /// 当从网络接收数据时的回调，每帧调用一次
    /// </summary>
    /// <param name="data">接收到的数据字节流，总长度为构造函数定义的200kb，并非所有的数据都是新的</param>
    /// <param name="dataLength">接收到的数据长度，表示data字节流数组中有多少数据是新接收到的，即0-dataLength之间的数据是刚接收到的</param>
    /// <returns>返回true表示当下载正在进行，返回false表示下载中止</returns>
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || data.Length == 0)
        {
            Debug.Log("没有获取到数据缓存!");
            return false;
        }

        fs.Write(data, 0, dataLength);
        _downedLength += dataLength;

        if (_eventProgress != null)
            _eventProgress.Invoke((float)_downedLength / _contentLength);   //通知进度消息

        return true;
    }

    /// <summary>
    /// 所有数据接收完成的回调，将临时文件保存为制定的文件名
    /// </summary>
    protected override void CompleteContent()
    {
		try
		{
			string CompleteFilePath = DirectoryPath + "/" + FileName;   //完整路径
			string TempFilePath = fs.Name;   //临时文件路径
			OnDispose();

			if (File.Exists(TempFilePath))
			{
				if (File.Exists(CompleteFilePath))
				{
					File.Delete(CompleteFilePath);
				}
				if (TempFilePath.Contains(Global.TablePackageName))
				{
					CoroutineUtil.DoCoroutine(MoveArchieveZipFile(TempFilePath, CompleteFilePath));
					return;
				}
				else
				{
					File.Move(TempFilePath, CompleteFilePath);
				}
			}
			else
			{
				Debug.Log("生成文件失败=>下载的文件不存在！");
			}
			if (_eventComplete != null)
				_eventComplete.Invoke(CompleteFilePath);
		}
		catch (Exception e)
		{
			//NetworkSystem.Instance.PostErrorLogToServer("DownloadHandler CompleteContent exception: " + e.Message, "");
		}
    }

	public IEnumerator MoveArchieveZipFile(string TempFilePath, string CompleteFilePath)
	{
		yield return new WaitForSeconds(0.2f);
		File.Move(TempFilePath, CompleteFilePath);

		if (_eventComplete != null)
			_eventComplete.Invoke(CompleteFilePath);
	}

    public void OnDispose()
    {
        if (fs != null)
        {
            fs.Close();
            fs.Dispose();
        }
    }

    /// <summary>
    /// 请求下载时，会先接收到文件的数据总量
    /// </summary>
    /// <param name="contentLength">如果是从网络上下载资源，则表示文件剩余下载的大小；如果是本地拷贝资源，则表示文件总长度</param>
    protected override void ReceiveContentLength(int contentLength)
    {
        _contentLength = contentLength + _downedLength;
        if (_eventTotalLength != null)
            _eventTotalLength.Invoke(_contentLength);
    }
}
