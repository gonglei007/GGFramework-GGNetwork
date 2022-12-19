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
    const string URL_WEB1 = "http://43.245.49.164:4210/slgLogin";
    const string URL_DATA = "";
    const string URL_GAME = "";

    [SetUp]
    public IEnumerator Start() {
        Debug.Log("Test started!");
        GameNetworkSystem.Instance.Init();
        httpDNS.Init("http://103.150.251.71/v1/dns/query");
        yield return null;
    }

    // A Test behaves as an ordinary method
    [Test, Order(2)]
    public void Test_GetURLByIP()
    {
        // Use the Assert class to test conditions
        string newUrl = httpDNS.GetURLByIP(URL_WEB);
        Assert.That(newUrl.Equals(URL_WEB1), "new ip is right!");
    }

    [UnityTest, Order(1)]
    public IEnumerator Test_ParseHost() {
        bool isFinished = false;
        httpDNS.ParseHost(DOMAIN_WEB, (string ip, HTTPDNS.EStatus status, string message) => {
            //Assert.That(false, "Error!");
            Debug.LogWarningFormat("ip:{0}-status:{1}-{2}", ip, status.ToString(), message);
            isFinished = true;
            Assert.That(string.IsNullOrEmpty(ip), "Wrong ip result!");
            Assert.That(status == HTTPDNS.EStatus.RET_SUCCESS, "status must be RET_SUCCESS!");

        });
        yield return new WaitWhile(()=> {
            //Debug.LogFormat("request finished?-{0}", isFinished);
            return !isFinished;
        });
    }
}
