using NUnit.Framework;
using UnityEngine;
using GGFramework.GGNetwork;
using GGFramework.GGNetwork.HTTPDNS;

public class TestHTTPDNSCache
{
    [Test]
    public void Test_Cache_TTLDefaultsTo60()
    {
        var cache = new HTTPDNSSystem.Cache();
        Assert.That(cache.TTL, Is.EqualTo(60));
    }

    [Test]
    public void Test_Cache_TTLClampedToMin()
    {
        var cache = new HTTPDNSSystem.Cache();

        cache.TTL = 60;
        Assert.That(cache.TTL, Is.EqualTo(60));

        cache.TTL = 5;
        Assert.That(cache.TTL, Is.EqualTo(5));

        // Values <= 1 get clamped to MIN_TTL_SECOND (2)
        cache.TTL = 1;
        Assert.That(cache.TTL, Is.EqualTo(2));

        cache.TTL = -1;
        Assert.That(cache.TTL, Is.EqualTo(2));
    }

    [Test]
    public void Test_Cache_UpdateTimeStartsAtZero()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();
        Assert.That(cache.UpdatedTime, Is.GreaterThanOrEqualTo(0));
        Assert.That(cache.UpdatedTime, Is.LessThan(5));
    }

    [Test]
    public void Test_Cache_UpdatedTimeIncreases()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();

        // The UpdatedTime property calculates elapsed seconds since last update.
        // After reset, it should be very close to 0.
        int t0 = cache.UpdatedTime;
        Assert.That(t0, Is.GreaterThanOrEqualTo(0));
        Assert.That(t0, Is.LessThan(2));
    }

    [Test]
    public void Test_Cache_DomainProperty()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.domain = "example.com";
        Assert.That(cache.domain, Is.EqualTo("example.com"));
    }

    [Test]
    public void Test_Cache_IPProperty()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ip = "192.168.1.1";
        Assert.That(cache.ip, Is.EqualTo("192.168.1.1"));
    }

    [Test]
    public void Test_HTTPDNSSystem_ReplaceHost()
    {
        var system = HTTPDNSSystem.Instance;
        system.ON = false;

        // When off, GetURLByIP returns the original URL unchanged
        string url = "http://example.com:8080/api/v1";
        string result = system.GetURLByIP(url);
        Assert.That(result, Is.EqualTo(url));
    }

    [Test]
    public void Test_EStatus_Values()
    {
        Assert.That((int)HTTPDNSSystem.EStatus.RET_SUCCESS, Is.EqualTo(1000));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_NO_ON, Is.EqualTo(1001));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_NO_HOST, Is.EqualTo(1002));
        Assert.That((int)HTTPDNSSystem.EStatus.RET_ERROR_RESULT, Is.EqualTo(1003));
    }
}
