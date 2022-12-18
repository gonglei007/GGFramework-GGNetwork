using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//using GGFramework.GGNetwork;

public class TestHTTPDNS
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestTranslateURL()
    {
        // Use the Assert class to test conditions
        //HTTPDNS httpDNS = new HTTPDNS();
        //TranslateURL
        Assert.That(false, "Error!");
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
