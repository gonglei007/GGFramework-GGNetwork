using System;
using NUnit.Framework;
using SimpleJson;
using UnityEngine.TestTools;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for EventManager: reqId -> callback map (responses) and
/// route -> subscriber list (pushes), including thread-safe snapshotting
/// and exception isolation.
/// </summary>
public class TestEventManager
{
    private static JsonObject Data(string key, string value)
    {
        var o = new JsonObject();
        o[key] = value;
        return o;
    }

    // ---------- Callbacks (responses) ----------

    [Test]
    public void Test_AddCallBack_ThenInvoke()
    {
        var mgr = new EventManager();
        JsonObject received = null;
        mgr.AddCallBack(1, d => received = d);

        bool ok = mgr.InvokeCallBack(1, Data("a", "b"));
        Assert.That(ok, Is.True);
        Assert.That(received, Is.Not.Null);
        Assert.That(received["a"].ToString(), Is.EqualTo("b"));
    }

    [Test]
    public void Test_InvokeCallBack_RemovesAfterFire()
    {
        var mgr = new EventManager();
        int count = 0;
        mgr.AddCallBack(2, d => count++);

        Assert.That(mgr.InvokeCallBack(2, null), Is.True);
        Assert.That(mgr.InvokeCallBack(2, null), Is.False, "Callback must be one-shot");
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Test_InvokeCallBack_UnknownId_ReturnsFalse()
    {
        var mgr = new EventManager();
        Assert.That(mgr.InvokeCallBack(999, null), Is.False);
    }

    [Test]
    public void Test_AddCallBack_ZeroId_Ignored()
    {
        var mgr = new EventManager();
        mgr.AddCallBack(0, d => { });
        Assert.That(mgr.InvokeCallBack(0, null), Is.False);
    }

    [Test]
    public void Test_AddCallBack_NullCallback_Ignored()
    {
        var mgr = new EventManager();
        mgr.AddCallBack(3, null);
        Assert.That(mgr.InvokeCallBack(3, null), Is.False);
    }

    [Test]
    public void Test_AddCallBack_DuplicateId_FirstWins()
    {
        var mgr = new EventManager();
        string which = null;
        mgr.AddCallBack(4, d => which = "first");
        mgr.AddCallBack(4, d => which = "second");

        mgr.InvokeCallBack(4, null);
        Assert.That(which, Is.EqualTo("first"));
    }

    [Test]
    public void Test_InvokeCallBack_ThrowingCallback_DoesNotPropagate()
    {
        LogAssert.ignoreFailingMessages = true;
        var mgr = new EventManager();
        mgr.AddCallBack(5, d => throw new InvalidOperationException("boom"));

        bool ok = false;
        Assert.DoesNotThrow(() => ok = mgr.InvokeCallBack(5, null));
        Assert.That(ok, Is.True);
    }

    // ---------- Events (pushes) ----------

    [Test]
    public void Test_AddOnEvent_MultipleSubscribers_AllInvoked()
    {
        var mgr = new EventManager();
        int count = 0;
        mgr.AddOnEvent("onChat", d => count++);
        mgr.AddOnEvent("onChat", d => count++);

        bool ok = mgr.InvokeOnEvent("onChat", Data("text", "hi"));
        Assert.That(ok, Is.True);
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Test_InvokeOnEvent_UnknownRoute_ReturnsFalse()
    {
        var mgr = new EventManager();
        Assert.That(mgr.InvokeOnEvent("nope", null), Is.False);
    }

    [Test]
    public void Test_AddOnEvent_EmptyName_Ignored()
    {
        var mgr = new EventManager();
        mgr.AddOnEvent("", d => { });
        mgr.AddOnEvent(null, d => { });
        Assert.That(mgr.InvokeOnEvent("", null), Is.False);
    }

    [Test]
    public void Test_AddOnEvent_NullCallback_Ignored()
    {
        var mgr = new EventManager();
        mgr.AddOnEvent("onChat", null);
        Assert.That(mgr.InvokeOnEvent("onChat", null), Is.False);
    }

    [Test]
    public void Test_InvokeOnEvent_PersistsAcrossMultipleInvocations()
    {
        var mgr = new EventManager();
        int count = 0;
        mgr.AddOnEvent("onTick", d => count++);

        mgr.InvokeOnEvent("onTick", null);
        mgr.InvokeOnEvent("onTick", null);
        Assert.That(count, Is.EqualTo(2), "Push subscriptions are not one-shot");
    }

    [Test]
    public void Test_InvokeOnEvent_ThrowingSubscriber_IsolatedAndOthersStillRun()
    {
        LogAssert.ignoreFailingMessages = true;
        var mgr = new EventManager();
        int secondRan = 0;
        mgr.AddOnEvent("onChat", d => throw new InvalidOperationException("boom"));
        mgr.AddOnEvent("onChat", d => secondRan++);

        Assert.DoesNotThrow(() => mgr.InvokeOnEvent("onChat", null));
        Assert.That(secondRan, Is.EqualTo(1));
    }

    [Test]
    public void Test_InvokeOnEvent_SubscriberAddingDuringInvoke_NoIterationCrash()
    {
        var mgr = new EventManager();
        int runs = 0;
        Action<JsonObject> handler = null;
        handler = d =>
        {
            runs++;
            // Adding during invocation must not corrupt the iteration (snapshot is used).
            mgr.AddOnEvent("onChat", d2 => { });
        };
        mgr.AddOnEvent("onChat", handler);

        Assert.DoesNotThrow(() => mgr.InvokeOnEvent("onChat", null));
        Assert.That(runs, Is.EqualTo(1));
    }

    // ---------- Dispose ----------

    [Test]
    public void Test_Dispose_ClearsCallbacksAndEvents()
    {
        var mgr = new EventManager();
        mgr.AddCallBack(7, d => { });
        mgr.AddOnEvent("onChat", d => { });

        mgr.Dispose();

        Assert.That(mgr.InvokeCallBack(7, null), Is.False);
        Assert.That(mgr.InvokeOnEvent("onChat", null), Is.False);
    }
}
