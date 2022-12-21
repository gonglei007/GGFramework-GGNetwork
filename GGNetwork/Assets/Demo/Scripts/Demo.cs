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
        //HttpNetworkSystem.Instance.ParamType = HttpNetworkSystem.EParamType.Text;
        HttpNetworkSystem.Instance.UIAdaptor.onDialog = (string title, string msg, bool retry, Action<bool> callback) =>{
            Debug.Log(title + " | " + msg + " | " + retry.ToString());
            QuestionDialogUI.Instance.ShowQuestion(title + " | " + msg, () => {
                // 如果回调传入true,就是让刚刚失败的操作重试。
                callback(true);
            }, () => {
                // Do things on No
            });
        };
        HttpNetworkSystem.Instance.LogAdaptor.onPostNetworkError = (string host, JsonObject param) => {
            string reportJson = SimpleJson.SimpleJson.SerializeObject(param);
            Debug.LogErrorFormat("Post error info to host. [{0}] | {1}", host, reportJson);
            EagleEye.TestNetwork(host, reportJson);
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
        const int XOffset = 120;
        const int YOffset = 60;
        // Good test
        if (GUI.Button(new Rect(XOffset, YOffset * 1, ButtonWidth, ButtonHeight), "HttpDNS Prepare"))
        {
            ServiceCenter.Instance.HTTPDNS.ParseHost("dev.gltop.com", (string ip, HTTPDNS.EStatus status, string message)=> {
                Debug.LogFormat("Host=>IP:{0}-status:{1}", ip, status);
                QuestionDialogUI.Instance.ShowQuestion(string.Format("Host=>IP:{0}-status:{1}-{2}", ip, status, message), () => { }, () => { });
            });
        }
        if (GUI.Button(new Rect(XOffset, YOffset * 2, ButtonWidth, ButtonHeight), "Good Http Get Request"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "url", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Good] | " + response.ToString(), () => {}, () => {});
            });
        }

        // Bad test
        if (GUI.Button(new Rect(XOffset, YOffset * 3, ButtonWidth, ButtonHeight), "[Bad-1]Http No Route!"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "no-route", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Bad] | " + response.ToString(), () => { }, () => { });
            });
        }
        if (GUI.Button(new Rect(XOffset, YOffset * 4, ButtonWidth, ButtonHeight), "[Bad-2]Http Server Resp Error!"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "erro1", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Bad] | " + response.ToString(), () => { }, () => { });
            });
        }
        if (GUI.Button(new Rect(XOffset, YOffset * 5, ButtonWidth, ButtonHeight), "[Bad-3]Http Bad URL!"))
        {
            JsonObject param = new JsonObject();
            HttpNetworkSystem.Instance.GetWebRequest(badHttpURL, "", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log(response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[Bad] | " + response.ToString(), () => { }, () => { });
            });
        }

        //if (GUI.Button(new Rect(XOffset, YOffset * 4, ButtonWidth, ButtonHeight), "Test Dialog"))
        //{
        //    QuestionDialogUI.Instance.ShowQuestion("Are you sure you want to quit the game?", () => {
        //        QuestionDialogUI.Instance.ShowQuestion("Are you really sure?", () => {
        //            Application.Quit();
        //            //EditorApplication.ExitPlaymode();
        //        }, () => {
        //            // Do nothing
        //        });
        //    }, () => {
        //        // Do nothing on No
        //    });
        //}
    }
}
