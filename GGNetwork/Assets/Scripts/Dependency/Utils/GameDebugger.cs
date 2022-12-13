using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using CodeStage.AdvancedFPSCounter;

#if UNITY_EDITOR
using UnityEditor;
# endif
/** 
* 简单的调试类。目前是向屏幕输出文字信息。
* TODO: GL - 记入文件或通过网络发送。
*/
public class GameDebugger : Singleton<GameDebugger> {

    public GameObject debugConsole = null;
	private const int	LineSize = 30;
	private Queue<string>	debugInfoQueue = new Queue<string>(LineSize);
	private string 		debugInfo = "";
	private int			infoCounter = 1;
	private GUIStyle	debugTextStyle = new GUIStyle();
    private static bool enable = false;

	public static bool	    Enable{
        get {
            return enable;
        }
        set {
            enable = value;
            if (GameDebugger.Instance.debugConsole != null) {
                GameDebugger.Instance.debugConsole.SetActive(enable);
            }
#if UNITY_EDITOR
			Debug.unityLogger.logEnabled = true;

            showLog = false;
            showFPS = false;
            if (GameDebugger.Instance.debugConsole != null)
            {
                GameDebugger.Instance.debugConsole.SetActive(false);
            }
#else
			Debug.unityLogger.logEnabled = enable;
            showLog = enable;
            showFPS = enable;
#endif
            try
            {
                if (enable)
                {
                    if (AFPSCounter.Instance == null)
                    {
                        var newCounterInstance = AFPSCounter.AddToScene();
                        AFPSCounter.Instance.fpsCounter.Anchor = CodeStage.AdvancedFPSCounter.Labels.LabelAnchor.UpperCenter;
                        AFPSCounter.Instance.memoryCounter.Anchor = CodeStage.AdvancedFPSCounter.Labels.LabelAnchor.UpperCenter;
                        AFPSCounter.Instance.deviceInfoCounter.Anchor = CodeStage.AdvancedFPSCounter.Labels.LabelAnchor.LowerLeft;
                    }
                }
                GameDebugger.Instance.ShowFPS(showFPS);
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
#if UNITY_EDITOR
	public static bool		showLog = false;
    public static bool      showFPS = false;
#else
	public static bool		showLog = true;
    public static bool      showFPS = true;
#endif
    /*
	private static GameDebugger instance;
	public static GameDebugger Instance
	{
		get
		{
			return instance;
		}
	}
    */

    /**
     * TODO: 具体的配置由外面传入。
    */
    public void Init()
    {
        GameDebugger.Enable = false;
        Application.logMessageReceived += (string logString, string stackTrace, LogType type) =>
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                string errorStr = "[error] " + logString + " | " + stackTrace;
                if (GameDebugger.Instance != null)
                    GameDebugger.Instance.PushLog(errorStr);
                //GONetworkManager.Instance.PostErrorLogToServer(errorStr, stackTrace);
            }
        };
    }

    void Awake(){
		//instance = this;
	}

	// Use this for initialization
	void Start () {
		debugTextStyle.normal.textColor = Color.red;
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showLog = !showLog;
        }
        if (Input.GetKeyDown(KeyCode.F2)) {
            showFPS = !showFPS;
            ShowFPS(showFPS);
        }
	}

    private void ShowFPS(bool show) {
        if (AFPSCounter.Instance) {
            if (show)
            {
                AFPSCounter.Instance.OperationMode = OperationMode.Normal;
            }
            else
            {
                AFPSCounter.Instance.OperationMode = OperationMode.Disabled;
            }
        }
    }

	void OnGUI()
	{
        return;
		if (showLog)
		{
			GUI.Label(new Rect(10, 10, 640, 480), debugInfo, debugTextStyle);
            /*
            GameObject cameraWalker = GameObject.Find("CameraWalker");
            GameObject cameraObject = GameObject.Find("MapCamera");
            if (cameraObject != null && cameraWalker != null)
            {
                Camera camera3D = cameraObject.GetComponent(typeof(Camera)) as Camera;
                if (camera3D != null) {
                    string cameraInfo = string.Format("Camera->\n<color={0}>\t POS:{1}\n\t ROT:{2}</color> \nWalker->\n\t POS:{3}\n\t ROT:{4}",
                        "#ff0000ff",
                        camera3D.transform.localPosition.ToString(),
                        camera3D.transform.localRotation.ToString(),
                        cameraWalker.transform.localPosition.ToString(),
                        cameraWalker.transform.localRotation.ToString()
                        );
                    GUI.Label(new Rect(Screen.width - 400, 10, 400, 400), cameraInfo, debugTextStyle);
                }
            }
            else {
                GUI.Label(new Rect(Screen.width - 200, 10, 200, 400), "Not found 3D Camera!!", debugTextStyle);
            }
            */
		}
	}

    public static void sPushLog(string info) {
        if (Instance != null)
        {
            Instance.PushLog(info);
        }
        else {
            Debug.LogWarning("GameDebugger instance is null!");
        }
    }

	public void PushLog(string info){
		Debug.Log(info);
		if(!Enable){
			return;
		}
        try
        {
            //info = string.Format("{0}-{1}", GOGameManager.frameCount, info);
            debugInfoQueue.Enqueue(info);
            if (debugInfoQueue.Count > LineSize)
            {
                debugInfoQueue.Dequeue();
            }
            string[] infoList = debugInfoQueue.ToArray();
            debugInfo = "";
            /*
            for(int i=infoList.Length-1; i>=0; --i){
                debugInfo += string.Format("{0}:{1}\n", (infoCounter -infoList.Length+1+i), infoList[i]);
            } */
            for (int i = 0; i < infoList.Length; ++i)
            {
                //debugInfo += string.Format("{0}:{1}\n", (infoCounter+i), infoList[i]);
                string colorValue = "";
                if (infoList[i].Contains("[error]"))
                {
                    colorValue = "#ff0000ff";
                }
                else if (infoList[i].Contains("[network]"))
                {
                    colorValue = "#ffff00ff";
                }
                else
                {
                    colorValue = "#00ffffff";
                }
                debugInfo += string.Format("<color={1}>> {0}</color>\n", infoList[i], colorValue);
            }
            infoCounter++;
        }
        catch (Exception e) {
            Debug.Log(e.ToString());
        }
	}

	public void PushLogFormat(string info, params object[] args){
		PushLog(string.Format(info, args));
	}
}
