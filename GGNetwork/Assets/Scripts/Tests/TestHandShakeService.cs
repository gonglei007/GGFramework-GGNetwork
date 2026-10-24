using System.Collections.Generic;
using NUnit.Framework;
using SimpleJson;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for HandShakeService handshake validation and side effects,
/// using a fake transport session (no real sockets involved).
/// </summary>
public class TestHandShakeService
{
    private class FakeSession : IProtocolSession
    {
        public readonly List<PackageType> bodyless = new List<PackageType>();
        public readonly List<PackageType> withBody = new List<PackageType>();
        public readonly List<WsNetWorkState> states = new List<WsNetWorkState>();

        public void Send(PackageType type, byte[] body) { withBody.Add(type); }
        public void Send(PackageType type) { bodyless.Add(type); }
        public void OnStateChanged(WsNetWorkState state) { states.Add(state); }
    }

    private static JsonObject ValidHandshake(int code, bool withDict, int heartbeat)
    {
        var sys = new JsonObject();
        if (withDict)
        {
            var dict = new JsonObject();
            dict["onChat"] = 3;
            sys["dict"] = dict;
        }
        if (heartbeat > 0) sys["heartbeat"] = heartbeat;

        var msg = new JsonObject();
        msg["code"] = code;
        msg["sys"] = sys;

        var user = new JsonObject();
        user["uid"] = "u1";
        msg["user"] = user;
        return msg;
    }

    [Test]
    public void Test_ProcessHandshakeData_Success()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        string error;
        bool ok = service.ProcessHandshakeData(ValidHandshake(200, true, 0), out error);

        Assert.That(ok, Is.True);
        Assert.That(error, Is.Null);
        Assert.That(service.MessageProtocol, Is.Not.Null);
        // ACK must be sent back to the server after a successful handshake.
        Assert.That(session.bodyless, Contains.Item(PackageType.PKG_HANDSHAKE_ACK));
        service.StopHeartBeat();
    }

    [Test]
    public void Test_ProcessHandshakeData_MissingCode_Fails()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        var msg = new JsonObject();
        msg["sys"] = new JsonObject();

        string error;
        bool ok = service.ProcessHandshakeData(msg, out error);
        Assert.That(ok, Is.False);
        Assert.That(error, Is.Not.Null);
        Assert.That(service.MessageProtocol, Is.Null);
    }

    [Test]
    public void Test_ProcessHandshakeData_Non200_Fails()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        string error;
        bool ok = service.ProcessHandshakeData(ValidHandshake(500, true, 0), out error);
        Assert.That(ok, Is.False);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void Test_ProcessHandshakeData_WithSmallHeartbeat_CreatesServiceButNoTimer()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        // 500ms < HeartBeatService minimum (1000ms) => service created but timer not started.
        string error;
        bool ok = service.ProcessHandshakeData(ValidHandshake(200, true, 500), out error);
        Assert.That(ok, Is.True);
        Assert.That(service.HeartBeatService, Is.Not.Null);
        service.StopHeartBeat();
    }

    [Test]
    public void Test_Request_SendsHandshakePackage()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        var user = new JsonObject();
        user["token"] = "abc";
        service.Request(user, result => { });

        Assert.That(session.withBody, Contains.Item(PackageType.PKG_HANDSHAKE));
    }

    [Test]
    public void Test_Request_ThenProcess_InvokesBusinessCallbackWithUser()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);

        JsonObject callbackUser = null;
        service.Request(new JsonObject(), result => callbackUser = result);

        string error;
        service.ProcessHandshakeData(ValidHandshake(200, false, 0), out error);

        Assert.That(callbackUser, Is.Not.Null);
        Assert.That(callbackUser["uid"].ToString(), Is.EqualTo("u1"));
        service.StopHeartBeat();
    }

    [Test]
    public void Test_Request_NullUser_DoesNotThrow()
    {
        var session = new FakeSession();
        var service = new HandShakeService(session);
        Assert.DoesNotThrow(() => service.Request(null, r => { }));
        Assert.That(session.withBody, Contains.Item(PackageType.PKG_HANDSHAKE));
    }

    [Test]
    public void Test_Constants()
    {
        Assert.That(HandShakeService.Version, Is.EqualTo("0.3.0"));
        Assert.That(HandShakeService.Type, Is.EqualTo("unity-websocket"));
    }
}
