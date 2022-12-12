using System;
using UnityEngine;
using GGFramework.GGNetwork;
using SimpleJson;

public class Demo : MonoBehaviour
{
    //private const string goodHttpURL = "https://www.baidu.com";
    private const string goodHttpURL = "http://119.28.57.87:4201";
    private const string badHttpURL = "http://no.gltop.com:8080";   //"https://api.apiopen.top/singlePoetry";
    // Start is called before the first frame update
    void Start()
    {
        HttpNetworkSystem.Instance.Init();
        NetworkSystem.Instance.Init();
        ServiceCenter.Instance.Init();
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
        if (GUI.Button(new Rect(100, 40, 300, 50), "Good Http Request Test"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log(response.ToString());
            });
        }
        if (GUI.Button(new Rect(100, 100, 300, 50), "Bad Http Request Test"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(badHttpURL, "", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log(response.ToString());
            });
        }

        if (GUI.Button(new Rect(100, 160, 300, 50), "Open Dialog"))
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
