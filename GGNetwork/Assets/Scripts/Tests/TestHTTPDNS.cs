using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GGFramework.GGNetwork;

public class TestHTTPDNS// : MonoBehaviour
{
    HTTPDNS httpDNS = new HTTPDNS();
    const string DOMAIN_WEB = "global.gotechgames.com";
    const string DOMAIN_DATA = "";
    const string DOMAIN_GAME = "";
    const string URL_WEB = "http://global.gotechgames.com:4210/slgLogin";
    const string URL_DATA = "";
    const string URL_GAME = "";

    [SetUp]
    public void Start() {
        Debug.Log("Test started!");
        GameNetworkSystem.Instance.Init();
        httpDNS.Init("http://103.150.251.71/v1/dns/query");
    }

    // A Test behaves as an ordinary method
    [UnityTest]
    public IEnumerator Test_TranslateURL()
    {
        // Use the Assert class to test conditions
        httpDNS.TranslateURL(URL_WEB, (string newURL, HTTPDNS.EStatus status) => {
            //Assert.That(false, "Error!");
            Debug.LogFormat("newUrl:{0}-status:{1}", newURL, status.ToString());
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_ParseHost() {
        bool isFinished = false;
        httpDNS.ParseHost(DOMAIN_WEB, (string ip, HTTPDNS.EStatus status) => {
            //Assert.That(false, "Error!");
            Debug.LogWarningFormat("ip:{0}-status:{1}", ip, status.ToString());
            isFinished = true;
            Assert.That(string.IsNullOrEmpty(ip), "Wrong ip result!");
            Assert.That(status == HTTPDNS.EStatus.RET_SUCCESS, "status must be RET_SUCCESS!");

        });
        yield return new WaitWhile(()=> {
            //Debug.LogFormat("request finished?-{0}", isFinished);
            return !isFinished;
        });
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
