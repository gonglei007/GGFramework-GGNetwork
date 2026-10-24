using System;
using NUnit.Framework;
using SimpleJson;
using GGFramework.GGNetwork;

/// <summary>
/// Tests for NetworkRequest: constructor variants, state machine transitions
/// and callback dispatch.
/// </summary>
public class TestNetworkRequest
{
    private class FakeNetworkCallback : INetworkCallback
    {
        public string lastResponse;
        public string lastModule;
        public string lastFunc;
        public int singleCallCount;
        public int moduleCallCount;

        public void Call(string response)
        {
            lastResponse = response;
            singleCallCount++;
        }

        public void Call(string module, string func, string response)
        {
            lastModule = module;
            lastFunc = func;
            lastResponse = response;
            moduleCallCount++;
        }
    }

    private static JsonObject Sample()
    {
        var o = new JsonObject();
        o["k"] = "v";
        return o;
    }

    // ---------- Constructors ----------

    [Test]
    public void Test_Constructor_RouteOnly_DefaultState()
    {
        var req = new NetworkRequest("route");
        Assert.That(req.route, Is.EqualTo("route"));
        Assert.That(req.State, Is.EqualTo(NetworkRequest.RequestStates.Initial));
        Assert.That(req.msgID, Is.EqualTo(0));
        Assert.That(req.timer, Is.EqualTo(0.0f));
    }

    [Test]
    public void Test_Constructor_Callback1_SetsType()
    {
        var req = new NetworkRequest((JsonObject d) => { }, Sample(), "route", Sample());
        Assert.That(req.Type, Is.EqualTo(NetworkRequest.CallbackType.CT_CALLBACK1));
        Assert.That(req.callback2, Is.Null);
    }

    [Test]
    public void Test_Constructor_Callback2_SetsType()
    {
        var req = new NetworkRequest((JsonObject a, JsonObject b) => { }, Sample(), "route", Sample());
        Assert.That(req.Type, Is.EqualTo(NetworkRequest.CallbackType.CT_CALLBACK2));
        Assert.That(req.callback, Is.Null);
    }

    [Test]
    public void Test_Constructor_LuaCallback_SetsType()
    {
        var fake = new FakeNetworkCallback();
        var req = new NetworkRequest(fake, Sample(), "route", Sample());
        Assert.That(req.Type, Is.EqualTo(NetworkRequest.CallbackType.CT_LUACALLBACK));
        Assert.That(req.iCallback, Is.SameAs(fake));
    }

    [Test]
    public void Test_Constructor_LuaCallback2_SetsModuleFunc()
    {
        var req = new NetworkRequest("mod", "func", Sample(), "route", Sample());
        Assert.That(req.Type, Is.EqualTo(NetworkRequest.CallbackType.CT_LUACALLBACK2));
        Assert.That(req.luaModule, Is.EqualTo("mod"));
        Assert.That(req.luaFunc, Is.EqualTo("func"));
    }

    // ---------- State machine ----------

    [Test]
    public void Test_ChangeState_InvokesCallbackOnce()
    {
        var req = new NetworkRequest("route");
        int count = 0;
        NetworkRequest.RequestStates observed = NetworkRequest.RequestStates.Initial;
        req.onStateChanged = s => { count++; observed = s; };

        req.ChangeState(NetworkRequest.RequestStates.Processing);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(observed, Is.EqualTo(NetworkRequest.RequestStates.Processing));
        Assert.That(req.State, Is.EqualTo(NetworkRequest.RequestStates.Processing));
    }

    [Test]
    public void Test_ChangeState_SameState_NoCallback()
    {
        var req = new NetworkRequest("route");
        int count = 0;
        req.onStateChanged = s => count++;

        req.ChangeState(NetworkRequest.RequestStates.Initial); // already Initial
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void Test_ChangeState_NullCallback_DoesNotThrow()
    {
        var req = new NetworkRequest("route");
        Assert.DoesNotThrow(() => req.ChangeState(NetworkRequest.RequestStates.Finished));
        Assert.That(req.State, Is.EqualTo(NetworkRequest.RequestStates.Finished));
    }

    // ---------- Update / timer ----------

    [Test]
    public void Test_Update_Processing_AccumulatesTimer()
    {
        var req = new NetworkRequest("route");
        req.ChangeState(NetworkRequest.RequestStates.Processing);
        req.Update(0.5f);
        req.Update(0.25f);
        Assert.That(req.timer, Is.EqualTo(0.75f).Within(0.0001f));
    }

    [Test]
    public void Test_Update_NonProcessing_TimerUnchanged()
    {
        var req = new NetworkRequest("route"); // Initial
        req.Update(1.0f);
        Assert.That(req.timer, Is.EqualTo(0.0f));
    }

    // ---------- DoCallback ----------

    [Test]
    public void Test_DoCallback_Callback1_InvokesWithData()
    {
        var data = Sample();
        JsonObject received = null;
        var req = new NetworkRequest(d => received = d, data, "route", Sample());

        req.DoCallback();
        Assert.That(received, Is.SameAs(data));
    }

    [Test]
    public void Test_DoCallback_Callback2_InvokesWithDataAndMsg()
    {
        var data = Sample();
        var msg = new JsonObject();
        msg["m"] = "1";
        JsonObject gotData = null, gotMsg = null;
        var req = new NetworkRequest((d, m) => { gotData = d; gotMsg = m; }, data, "route", msg);

        req.DoCallback();
        Assert.That(gotData, Is.SameAs(data));
        Assert.That(gotMsg, Is.SameAs(msg));
    }

    [Test]
    public void Test_DoCallback_LuaCallback_InvokesWithString()
    {
        var fake = new FakeNetworkCallback();
        var data = Sample();
        var req = new NetworkRequest(fake, data, "route", Sample());

        req.DoCallback();
        Assert.That(fake.singleCallCount, Is.EqualTo(1));
        Assert.That(fake.lastResponse, Is.EqualTo(data.ToString()));
    }
}
