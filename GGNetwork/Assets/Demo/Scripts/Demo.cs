using System;
using System.Collections.Generic;
using UnityEngine;
using GGFramework.GGNetwork;
using GGFramework.GGNetwork.HTTPDNS;
using SimpleJson;

/// <summary>
/// Demo showcasing GGNetwork features:
/// - System initialization
/// - HTTP GET/POST requests
/// - HTTPDNS host-to-IP resolution
/// - Exception actions (ConfirmRetry, Silence, Tips, Ignore)
/// - UI & Log adaptors
/// </summary>
public class Demo : MonoBehaviour
{
    private const string goodHttpURL = "http://dev.gltop.com:3000";
    private const string badHttpURL = "http://no.gltop.com:8080";
    private const string testDomain = "data-cy.hkqingyi.com";
    private const string HttpDNSAPIHost = "119.8.61.184:40021";
    private string[] testDomains = new string[]{
        "data-cy.hkqingyi.com",
        "data-cy.gotechgames.com",
    };

    private string statusText = "Ready";

    void Start()
    {
        // 1. Initialize game network system
        GameNetworkSystem.Instance.Init();

        // 2. Configure HTTPDNS
        HTTPDNSSystem.Instance.SetAPIHost(HttpDNSAPIHost);

        // 3. Enable parameter preprocessing (signing + timestamp)
        HttpNetworkSystem.Instance.CheckLogicErrorCode = false;
        // HttpNetworkSystem.Instance.EnablePreProcessParam = true; // Uncomment to enable

        // 4. Bind text localization adaptor
        HttpNetworkSystem.Instance.UIAdaptor.onGetText = (string text) => {
            // string retText = Localization.GetText(text);
            return text;
        };

        // 5. Bind dialog adaptor - shown on network exceptions
        HttpNetworkSystem.Instance.UIAdaptor.onDialog = (string title, string msg, bool retry, Action<bool> callback) =>{
            Debug.Log($"[Dialog] {title} | {msg} | retry={retry}");
            QuestionDialogUI.Instance.ShowQuestion(title + " | " + msg, () => {
                callback(true);  // Retry
            }, () => {
                // User cancelled
            });
        };

        // 6. Bind waiting indicator
        HttpNetworkSystem.Instance.UIAdaptor.onWaiting = (bool waiting) => {
            statusText = waiting ? "Loading..." : "Ready";
            Debug.Log($"[Waiting] {waiting}");
        };

        // 7. Bind network error logging adaptor
        HttpNetworkSystem.Instance.LogAdaptor.onPostNetworkError = (string host, JsonObject param) => {
            string reportJson = SimpleJson.SimpleJson.SerializeObject(param);
            Debug.LogErrorFormat("[Network Error] host:{0} | {1}", host, reportJson);
            EagleEye.TestNetwork(host, reportJson);
        };

        // 8. Bind error logging adaptor
        HttpNetworkSystem.Instance.LogAdaptor.onPostError = (string id, string info) => {
            Debug.LogError($"[Error Report] {id}: {info}");
        };
    }

    void OnGUI()
    {
        const int ButtonWidth = 320;
        const int ButtonHeight = 45;
        const int XOffset = 120;
        const int YOffset = 55;

        // Status display
        GUI.Label(new Rect(XOffset, 10, ButtonWidth, 25), $"Status: {statusText}");

        // ========== HTTPDNS ==========
        GUI.Label(new Rect(XOffset, YOffset * 0 + 20, ButtonWidth, 20), "--- HTTPDNS ---");

        if (GUI.Button(new Rect(XOffset, YOffset * 1, ButtonWidth, ButtonHeight), "HttpDNS: Batch Resolve Hosts"))
        {
            HTTPDNSSystem.Instance.ParseHosts(testDomains, (List<HTTPDNSSystem.Cache> caches, HTTPDNSSystem.EStatus status, string message)=> {
                Debug.LogFormat("Host=>IP count:{0} status:{1}", caches.Count, status);
                QuestionDialogUI.Instance.ShowQuestion($"Resolved {caches.Count} hosts", () => { }, () => { });
            });
        }

        // ========== HTTP GET ==========
        GUI.Label(new Rect(XOffset, YOffset * 2 + 15, ButtonWidth, 20), "--- HTTP GET ---");

        if (GUI.Button(new Rect(XOffset, YOffset * 3, ButtonWidth, ButtonHeight), "GET: Good Request (ConfirmRetry)"))
        {
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "url", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log("[GET OK] " + response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[GET OK] " + response.ToString(), () => {}, () => {});
            });
        }

        if (GUI.Button(new Rect(XOffset, YOffset * 4, ButtonWidth, ButtonHeight), "GET: Silence (no UI feedback)"))
        {
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "url", HttpNetworkSystem.ExceptionAction.Silence, (JsonObject response)=>{
                Debug.Log("[GET Silence] " + response.ToString());
            });
        }

        // ========== HTTP POST ==========
        GUI.Label(new Rect(XOffset, YOffset * 5 + 10, ButtonWidth, 20), "--- HTTP POST ---");

        if (GUI.Button(new Rect(XOffset, YOffset * 6, ButtonWidth, ButtonHeight), "POST: JSON Body Request"))
        {
            JsonObject param = new JsonObject();
            param["name"] = "test";
            param["value"] = 123;
            HttpNetworkSystem.Instance.PostWebRequest(goodHttpURL, "api/endpoint", param, HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log("[POST OK] " + response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[POST OK] " + response.ToString(), () => {}, () => {});
            });
        }

        if (GUI.Button(new Rect(XOffset, YOffset * 7, ButtonWidth, ButtonHeight), "POST: Form-Encoded Request"))
        {
            HttpNetworkSystem.Instance.PostWebRequest(goodHttpURL, "api/form", "key1=val1&key2=val2", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response)=>{
                Debug.Log("[POST Form] " + response.ToString());
            });
        }

        // ========== Error Tests ==========
        GUI.Label(new Rect(XOffset, YOffset * 8 + 5, ButtonWidth, 20), "--- Error Scenarios ---");

        if (GUI.Button(new Rect(XOffset, YOffset * 9, ButtonWidth, ButtonHeight), "ERR: No Route (ConfirmRetry)"))
        {
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "no-route", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log("[ERR-1] " + response.ToString());
            });
        }

        if (GUI.Button(new Rect(XOffset, YOffset * 10, ButtonWidth, ButtonHeight), "ERR: Server Error (ConfirmRetry)"))
        {
            HttpNetworkSystem.Instance.GetWebRequest(goodHttpURL, "erro1", HttpNetworkSystem.ExceptionAction.ConfirmRetry, (JsonObject response) => {
                Debug.Log("[ERR-2] " + response.ToString());
            });
        }

        if (GUI.Button(new Rect(XOffset, YOffset * 11, ButtonWidth, ButtonHeight), "ERR: Bad URL (Ignore exception)"))
        {
            HttpNetworkSystem.Instance.GetWebRequest(badHttpURL, "", HttpNetworkSystem.ExceptionAction.Ignore, (JsonObject response) => {
                Debug.Log("[ERR-3] " + response.ToString());
                QuestionDialogUI.Instance.ShowQuestion("[ERR Ignored] " + response.ToString(), () => { }, () => { });
            });
        }

        // ========== Utilities ==========
        GUI.Label(new Rect(XOffset, YOffset * 12 + 5, ButtonWidth, 20), "--- Utilities ---");

        if (GUI.Button(new Rect(XOffset, YOffset * 13, ButtonWidth, ButtonHeight), "Util: Get Timestamp"))
        {
            long ts = NetworkUtil.GetTimeStamp();
            QuestionDialogUI.Instance.ShowQuestion($"Timestamp: {ts}", () => { }, () => { });
        }

        if (GUI.Button(new Rect(XOffset, YOffset * 14, ButtonWidth, ButtonHeight), "Util: File Size Formatting"))
        {
            string size1 = NetworkUtil.GetFileLengthString(1024);      // 1 KB
            string size2 = NetworkUtil.GetFileLengthString(1048576);    // 1 MB
            QuestionDialogUI.Instance.ShowQuestion($"1024 = {size1}\n1048576 = {size2}", () => { }, () => { });
        }
    }
}
