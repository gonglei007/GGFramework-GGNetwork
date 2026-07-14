using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GGFramework.GGNetwork;
using GGFramework.GGNetwork.HTTPDNS;

public class TestHTTPDNS
{
    [SetUp]
    public void Start()
    {
        Debug.Log("Test started!");
    }

    [Test]
    public void Test_HTTPDNSSystem_Cache_TTL()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.TTL = 60;
        Assert.That(cache.TTL, Is.EqualTo(60));

        // TTL values <= 1 should be clamped to MIN_TTL_SECOND (2)
        cache.TTL = 1;
        Assert.That(cache.TTL, Is.EqualTo(2));

        cache.TTL = 0;
        Assert.That(cache.TTL, Is.EqualTo(2));

        cache.TTL = -1;
        Assert.That(cache.TTL, Is.EqualTo(2));
    }

    [Test]
    public void Test_HTTPDNSSystem_Cache_ResetUpdateTime()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();
        Assert.That(cache.UpdatedTime, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Test_HTTPDNSSystem_Cache_DomainAndIP()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.domain = "api.game.com";
        cache.ip = "10.0.0.1";
        Assert.That(cache.domain, Is.EqualTo("api.game.com"));
        Assert.That(cache.ip, Is.EqualTo("10.0.0.1"));
    }

    [Test]
    public void Test_HTTPDNSFactory_CreatesCorrectProvider()
    {
        var dns = HTTPDNSFactory.CreateHTTPDNS(HTTPDNSFactory.Provider.CY);
        Assert.That(dns, Is.Not.Null);
        Assert.That(dns, Is.InstanceOf<CyHTTPDNS>());
    }

    [Test]
    public void Test_HTTPDNSFactory_UnknownProviderReturnsNull()
    {
        var dns = HTTPDNSFactory.CreateHTTPDNS(HTTPDNSFactory.Provider.Ali);
        Assert.That(dns, Is.Null);
    }

    [Test]
    public void Test_HTTPDNSSystem_GetURLByIP_ReturnsOriginalWhenOff()
    {
        var system = HTTPDNSSystem.Instance;
        string url = "http://example.com/path";
        system.ON = false;
        string result = system.GetURLByIP(url);
        Assert.That(result, Is.EqualTo(url));
    }

    [Test]
    public void Test_HTTPDNSSystem_EStatus_Values()
    {
        Assert.That((int)HTTPDNSSystem.EStatus.RET_SUCCESS, Is.EqualTo(1000));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_NO_ON, Is.EqualTo(1001));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_NO_HOST, Is.EqualTo(1002));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_ERROR_RESULT, Is.EqualTo(1003));
    }
}
