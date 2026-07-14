using NUnit.Framework;
using UnityEngine;
using SimpleJson;
using GGFramework.GGNetwork;

public class TestNetworkUtil
{
    [Test]
    public void Test_GetFileLengthString_Bytes()
    {
        Assert.That(NetworkUtil.GetFileLengthString(500), Is.EqualTo("500 B"));
    }

    [Test]
    public void Test_GetFileLengthString_KB()
    {
        Assert.That(NetworkUtil.GetFileLengthString(2048), Is.EqualTo("2 KB"));
    }

    [Test]
    public void Test_GetFileLengthString_MB()
    {
        Assert.That(NetworkUtil.GetFileLengthString(1048576), Is.EqualTo("1 MB"));
    }

    [Test]
    public void Test_GetFileLengthString_GB()
    {
        Assert.That(NetworkUtil.GetFileLengthString(1073741824), Is.EqualTo("1 GB"));
    }

    [Test]
    public void Test_GetFileLengthString_TB()
    {
        Assert.That(NetworkUtil.GetFileLengthString(1099511627776), Is.EqualTo("1 TB"));
    }

    [Test]
    public void Test_GetFileLengthString_Zero()
    {
        Assert.That(NetworkUtil.GetFileLengthString(0), Is.EqualTo("0 B"));
    }

    [Test]
    public void Test_GetHostFromUrl_Standard()
    {
        string host = NetworkUtil.GetHostFromUrl("http://example.com:8080/path");
        Assert.That(host, Is.EqualTo("example.com"));
    }

    [Test]
    public void Test_GetHostFromUrl_Https()
    {
        string host = NetworkUtil.GetHostFromUrl("https://api.test.com/v1/resource");
        Assert.That(host, Is.EqualTo("api.test.com"));
    }

    [Test]
    public void Test_GetIPFromHost_InvalidHost()
    {
        string ip = NetworkUtil.GetIPFromHost("this-host-does-not-exist.invalid");
        Assert.That(ip, Is.EqualTo("0.0.0.0"));
    }

    [Test]
    public void Test_Sign_JsonObject_Basic()
    {
        JsonObject msg = new JsonObject();
        msg["key1"] = "value1";
        msg["key2"] = "value2";
        string sign = NetworkUtil.Sign(msg, "testSecret");
        Assert.That(sign, Is.Not.Null);
        Assert.That(sign.Length, Is.EqualTo(32)); // MD5 hex
    }

    [Test]
    public void Test_Sign_JsonObject_Deterministic()
    {
        // Same input should produce same output regardless of key order
        JsonObject msg1 = new JsonObject();
        msg1["a"] = "1";
        msg1["b"] = "2";

        JsonObject msg2 = new JsonObject();
        msg2["b"] = "2";
        msg2["a"] = "1";

        string sign1 = NetworkUtil.Sign(msg1, "secret");
        string sign2 = NetworkUtil.Sign(msg2, "secret");
        Assert.That(sign1, Is.EqualTo(sign2));
    }

    [Test]
    public void Test_Sign_JsonObject_NullInput()
    {
        string sign = NetworkUtil.Sign((JsonObject)null, "secret");
        Assert.That(sign, Is.Null);
    }

    [Test]
    public void Test_Sign_JsonObject_EmptyInput()
    {
        string sign = NetworkUtil.Sign(new JsonObject(), "secret");
        Assert.That(sign, Is.Null);
    }

    [Test]
    public void Test_GetTimeStamp_ReturnsPositive()
    {
        long ts = NetworkUtil.GetTimeStamp();
        Assert.That(ts, Is.GreaterThan(0));
    }

    [Test]
    public void Test_Sign_StringMessage_WithParams()
    {
        string message = "/api/test?param1=value1&param2=value2";
        string sign = NetworkUtil.Sign(message, "secret");
        Assert.That(sign, Is.Not.Null);
    }
}
