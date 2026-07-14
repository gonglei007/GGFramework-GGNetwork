using NUnit.Framework;
using UnityEngine;
using GGFramework.GGNetwork;
using GGFramework.GGNetwork.HTTPDNS;

/// <summary>
/// Tests for HTTPDNSSystem.Cache lifecycle and timeout behavior.
/// HTTPDNS factory/provider tests are in TestHTTPDNS.cs.
/// </summary>
public class TestHTTPDNSCache
{
    [Test]
    public void Test_Cache_Defaults()
    {
        var cache = new HTTPDNSSystem.Cache();
        Assert.That(cache.TTL, Is.EqualTo(60));
        Assert.That(cache.domain, Is.Null);
        Assert.That(cache.ip, Is.Null);
    }

    [Test]
    public void Test_Cache_UpdateTimeStartsFresh()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();
        Assert.That(cache.UpdatedTime, Is.GreaterThanOrEqualTo(0));
        Assert.That(cache.UpdatedTime, Is.LessThan(5), "Should be fresh (less than 5s)");
    }

    [Test]
    public void Test_Cache_MultipleResets()
    {
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();
        cache.ResetUpdateTime();
        Assert.That(cache.UpdatedTime, Is.GreaterThanOrEqualTo(0));
        Assert.That(cache.UpdatedTime, Is.LessThan(2));
    }

    [Test]
    public void Test_Cache_TTLClampBoundary()
    {
        var cache = new HTTPDNSSystem.Cache();

        // Value 2 should be allowed (equals MIN_TTL_SECOND)
        cache.TTL = 2;
        Assert.That(cache.TTL, Is.EqualTo(2));

        // Value 5 should pass through
        cache.TTL = 5;
        Assert.That(cache.TTL, Is.EqualTo(5));

        // Value 1 should clamp to 2
        cache.TTL = 1;
        Assert.That(cache.TTL, Is.EqualTo(2));

        // Negative should clamp to 2
        cache.TTL = -5;
        Assert.That(cache.TTL, Is.EqualTo(2));

        // Large values pass through
        cache.TTL = 3600;
        Assert.That(cache.TTL, Is.EqualTo(3600));
    }

    [Test]
    public void Test_Cache_IsExpiredLogic()
    {
        // Cache expiration: UpdatedTime > TTL means cache needs refresh.
        // A freshly reset cache should NOT be expired (UpdatedTime ~0, TTL >= 2).
        var cache = new HTTPDNSSystem.Cache();
        cache.ResetUpdateTime();
        cache.TTL = 60;
        Assert.That(cache.UpdatedTime, Is.LessThan(cache.TTL),
            "Fresh cache should not be expired");
    }
}
