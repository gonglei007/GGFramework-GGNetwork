using System;
using UnityEngine;
using GGFramework.GGNetwork;
using SimpleJson;

public class Demo : MonoBehaviour
{
    //private const string goodHttpURL = "https://www.baidu.com";
    private const string goodHttpURL = "http://dev.gltop.com:3000";
    private const string badHttpURL = "http://no.gltop.com:8080";   //"https://api.apiopen.top/singlePoetry";
    // Start is called before the first frame update
    void Start()
    {
        GameNetworkSystem.Instance.Init();
        HttpNetworkSystem.Instance.ParamType = HttpNetworkSystem.EParamType.Text;
        HttpNetworkSystem.Instance.UIAdaptor.onDialog = (string title, string msg, bool retry, Action<bool> callback) =>{
            Debug.Log(title + " | " + msg + " | " + retry.ToString());
            QuestionDialogUI.Instance.ShowQuestion(title + " | " + msg, () => {
                // 如果回调传入true,就是让刚刚失败的操作重试。
                callback(true);
            }, () => {
                // Do things on No
            });
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnGUI()
    {
        const int ButtonWidth = 300;
        const int ButtonHeight = 50;
        if (GUI.Button(new Rect(100, 40, ButtonWidth, ButtonHeight), "Good Http Get Request"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "url", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Good] | " + response.ToString(), () => {}, () => {});
            });
        }
        if (GUI.Button(new Rect(100, 100, ButtonWidth, ButtonHeight), "Bad Http Get Request"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(badHttpURL, "", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Bad] | " + response.ToString(), () => { }, () => { });
            });
        }

        if (GUI.Button(new Rect(100, 160, ButtonWidth, ButtonHeight), "Test Dialog"))
        {
            QuestionDialogUI.Instance.ShowQuestion("Are you sure you want to quit the game?", () => {
                QuestionDialogUI.Instance.ShowQuestion("Are you really sure?", () => {
                    Application.Quit();
                    //EditorApplication.ExitPlaymode();
                }, () => {
                    // Do nothing
                });
            }, () => {
                // Do nothing on No
            });
        }
    }
}
