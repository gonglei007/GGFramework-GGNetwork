#if !GGNETWORK_BESTHTTP
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestOptionalBestHTTP
{
    [Test]
    public void SelectingMissingBackendFailsExplicitly()
    {
        var factory = new BestHTTPFactory();
        Assert.Throws<NotSupportedException>(() => factory.CreateHTTPRequest(new Uri("http://localhost")));
        Assert.Throws<NotSupportedException>(() => factory.CreateHTTPResponse());
        Assert.Throws<NotSupportedException>(() => EagleEye.SetProxy("http://localhost:8080"));
    }

    [Test]
    public void ConfiguredDiagnosticsRemainDisabledWithoutPlugin()
    {
        try
        {
            LogAssert.Expect(LogType.Warning, "EagleEye requires BestHTTP and GGNETWORK_BESTHTTP; diagnostics remain disabled.");
            EagleEye.Init("test", "test", "https://example.invalid");
            Assert.That(EagleEye.On, Is.False);
            EagleEye.TestNetwork("local-only");
        }
        finally { EagleEye.On = false; }
    }
}
#endif
