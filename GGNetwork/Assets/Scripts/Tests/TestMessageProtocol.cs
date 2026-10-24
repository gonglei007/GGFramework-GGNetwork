using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SimpleJson;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for MessageProtocol: the Pomelo application-layer message codec
/// (request/response/notify/push), including route-dictionary compression and
/// the reqId -> route mapping used to restore a response's route.
/// </summary>
public class TestMessageProtocol
{
    private const string Route = "connector.entryHandler.query";

    private static byte[] JsonBytes(JsonObject obj)
    {
        return Encoding.UTF8.GetBytes(obj != null ? obj.ToString() : "{}");
    }

    private static JsonObject SampleMsg()
    {
        var msg = new JsonObject();
        msg["hello"] = "world";
        msg["n"] = 42;
        return msg;
    }

    // ---------- Constants ----------

    [Test]
    public void Test_Constants()
    {
        Assert.That(MessageProtocol.MSG_Route_Limit, Is.EqualTo(255));
        Assert.That(MessageProtocol.MSG_Route_Mask, Is.EqualTo(0x01));
        Assert.That(MessageProtocol.MSG_Type_Mask, Is.EqualTo(0x07));
    }

    // ---------- Encode ----------

    [Test]
    public void Test_Encode_Request_UncompressedRoute()
    {
        var protocol = new MessageProtocol(null);
        byte[] result = protocol.Encode(Route, 1, SampleMsg());

        // flag: type REQUEST(0)<<1 == 0, route not compressed => flag == 0
        Assert.That(result[0], Is.EqualTo(0));
        // next byte is varint id == 1
        Assert.That(result[1], Is.EqualTo(1));
        // then route length + route bytes
        Assert.That(result[2], Is.EqualTo(Encoding.UTF8.GetByteCount(Route)));
        string route = Encoding.UTF8.GetString(result, 3, result[2]);
        Assert.That(route, Is.EqualTo(Route));
    }

    [Test]
    public void Test_Encode_Notify_FlagHasNotifyBit()
    {
        var protocol = new MessageProtocol(null);
        byte[] result = protocol.Encode(Route, 0, SampleMsg());

        // flag: NOTIFY(1)<<1 == 2, uncompressed route => flag == 2
        Assert.That(result[0], Is.EqualTo(2));
        // No id byte: immediately route length.
        Assert.That(result[1], Is.EqualTo(Encoding.UTF8.GetByteCount(Route)));
    }

    [Test]
    public void Test_Encode_CompressedRoute_SetsRouteMaskAndTwoByteIndex()
    {
        var dict = new JsonObject();
        dict[Route] = 7; // route id 7
        var protocol = new MessageProtocol(dict);

        // notify + compressed => flag = NOTIFY(1)<<1 | ROUTE_MASK = 2 | 1 = 3
        byte[] result = protocol.Encode(Route, 0, SampleMsg());
        Assert.That(result[0], Is.EqualTo(3));
        // 2-byte big-endian route index
        Assert.That(result[1], Is.EqualTo(0));
        Assert.That(result[2], Is.EqualTo(7));
    }

    [Test]
    public void Test_Encode_NullMsg_ProducesEmptyJsonBody()
    {
        var protocol = new MessageProtocol(null);
        byte[] result = protocol.Encode(Route, 0, null);

        // Notify frame layout: [flag][routeLen(1)][route(routeLen)][body]
        int bodyStart = 2 + result[1];
        string tail = Encoding.UTF8.GetString(result, bodyStart, result.Length - bodyStart);
        Assert.That(tail, Is.EqualTo("{}"));
    }

    [Test]
    public void Test_Encode_RouteTooLong_Throws()
    {
        var protocol = new MessageProtocol(null);
        string longRoute = new string('a', 256);
        Assert.Throws<Exception>(() => protocol.Encode(longRoute, 0, SampleMsg()));
    }

    [Test]
    public void Test_Encode_RouteAtLimit_DoesNotThrow()
    {
        var protocol = new MessageProtocol(null);
        string route = new string('a', 255);
        Assert.DoesNotThrow(() => protocol.Encode(route, 0, SampleMsg()));
    }

    // ---------- Decode (Response) ----------

    [Test]
    public void Test_Decode_Response_RestoresRouteFromReqMap()
    {
        var protocol = new MessageProtocol(null);
        // Register reqId -> route by encoding a request first.
        protocol.Encode(Route, 1, SampleMsg());

        // Build a response buffer: flag = RESPONSE(2)<<1 = 4, then varint id, then JSON body.
        var respBody = new JsonObject();
        respBody["code"] = 200;
        respBody["data"] = "ok";

        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_RESPONSE) << 1)); // 4
        buf.AddRange(WsEncoder.EncodeUInt32(1));
        buf.AddRange(JsonBytes(respBody));

        Message msg = protocol.Decode(buf.ToArray());
        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.type, Is.EqualTo(MessageType.MSG_RESPONSE));
        Assert.That(msg.id, Is.EqualTo(1u));
        Assert.That(msg.route, Is.EqualTo(Route));
        Assert.That(Convert.ToInt32(msg.data["code"]), Is.EqualTo(200));
        Assert.That(msg.data["data"].ToString(), Is.EqualTo("ok"));
    }

    [Test]
    public void Test_Decode_Response_ConsumedOnce_SecondReturnsNull()
    {
        var protocol = new MessageProtocol(null);
        protocol.Encode(Route, 5, SampleMsg());

        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_RESPONSE) << 1));
        buf.AddRange(WsEncoder.EncodeUInt32(5));
        buf.AddRange(JsonBytes(SampleMsg()));

        Assert.That(protocol.Decode(buf.ToArray()), Is.Not.Null);
        // reqMap entry removed after first decode, so the same id no longer resolves.
        Assert.That(protocol.Decode(buf.ToArray()), Is.Null);
    }

    [Test]
    public void Test_Decode_Response_UnknownId_ReturnsNull()
    {
        var protocol = new MessageProtocol(null);
        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_RESPONSE) << 1));
        buf.AddRange(WsEncoder.EncodeUInt32(999)); // never registered
        buf.AddRange(JsonBytes(SampleMsg()));

        Assert.That(protocol.Decode(buf.ToArray()), Is.Null);
    }

    // ---------- Decode (Push) ----------

    [Test]
    public void Test_Decode_Push_UncompressedRoute()
    {
        var protocol = new MessageProtocol(null);
        string pushRoute = "onChat";
        var body = new JsonObject();
        body["text"] = "hi";

        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_PUSH) << 1)); // PUSH(3)<<1 = 6, bit0=0
        buf.Add((byte)Encoding.UTF8.GetByteCount(pushRoute));
        buf.AddRange(Encoding.UTF8.GetBytes(pushRoute));
        buf.AddRange(JsonBytes(body));

        Message msg = protocol.Decode(buf.ToArray());
        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.type, Is.EqualTo(MessageType.MSG_PUSH));
        Assert.That(msg.route, Is.EqualTo(pushRoute));
        Assert.That(msg.id, Is.EqualTo(0u));
        Assert.That(msg.data["text"].ToString(), Is.EqualTo("hi"));
    }

    [Test]
    public void Test_Decode_Push_CompressedRoute()
    {
        var dict = new JsonObject();
        dict["onMove"] = 12;
        var protocol = new MessageProtocol(dict);

        var buf = new List<byte>();
        buf.Add((byte)((((int)MessageType.MSG_PUSH) << 1) | 1)); // 6 | 1 = 7
        buf.Add(0);  // high byte of route id
        buf.Add(12); // low byte
        buf.AddRange(JsonBytes(SampleMsg()));

        Message msg = protocol.Decode(buf.ToArray());
        Assert.That(msg, Is.Not.Null);
        Assert.That(msg.route, Is.EqualTo("onMove"));
    }

    [Test]
    public void Test_Decode_Push_UnknownCompressedRoute_ReturnsNull()
    {
        var protocol = new MessageProtocol(null); // empty dictionary
        var buf = new List<byte>();
        buf.Add((byte)((((int)MessageType.MSG_PUSH) << 1) | 1));
        buf.Add(0);
        buf.Add(77); // not in abbrs
        buf.AddRange(JsonBytes(SampleMsg()));

        Assert.That(protocol.Decode(buf.ToArray()), Is.Null);
    }

    [Test]
    public void Test_Decode_Push_TruncatedRoute_ReturnsNull()
    {
        var protocol = new MessageProtocol(null);
        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_PUSH) << 1));
        buf.Add(20); // claims 20-byte route but buffer is shorter
        buf.AddRange(Encoding.UTF8.GetBytes("short"));

        Assert.That(protocol.Decode(buf.ToArray()), Is.Null);
    }

    // ---------- Decode (other / edge cases) ----------

    [Test]
    public void Test_Decode_RequestType_ReturnsNull()
    {
        var protocol = new MessageProtocol(null);
        // A REQUEST-typed frame is not something the client should receive/decode.
        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_REQUEST) << 1)); // 0
        buf.AddRange(JsonBytes(SampleMsg()));
        Assert.That(protocol.Decode(buf.ToArray()), Is.Null);
    }

    [Test]
    public void Test_Decode_NullOrEmpty_ReturnsNull()
    {
        var protocol = new MessageProtocol(null);
        Assert.That(protocol.Decode(null), Is.Null);
        Assert.That(protocol.Decode(new byte[0]), Is.Null);
    }

    [Test]
    public void Test_Decode_InvalidJsonBody_ProducesErrorPayload()
    {
        var protocol = new MessageProtocol(null);
        protocol.Encode(Route, 1, SampleMsg());

        var buf = new List<byte>();
        buf.Add((byte)(((int)MessageType.MSG_RESPONSE) << 1));
        buf.AddRange(WsEncoder.EncodeUInt32(1));
        buf.AddRange(Encoding.UTF8.GetBytes("this is not json {"));

        Message msg = protocol.Decode(buf.ToArray());
        Assert.That(msg, Is.Not.Null);
        Assert.That(Convert.ToInt32(msg.data["code"]), Is.EqualTo(500));
        Assert.That(msg.data.ContainsKey("msg"), Is.True);
    }

    // ---------- Message ----------

    [Test]
    public void Test_Message_Constructor()
    {
        var data = SampleMsg();
        var m = new Message(MessageType.MSG_PUSH, 3u, "route", data);
        Assert.That(m.type, Is.EqualTo(MessageType.MSG_PUSH));
        Assert.That(m.id, Is.EqualTo(3u));
        Assert.That(m.route, Is.EqualTo("route"));
        Assert.That(m.data, Is.SameAs(data));
    }

    [Test]
    public void Test_MessageType_Values()
    {
        Assert.That((int)MessageType.MSG_REQUEST, Is.EqualTo(0));
        Assert.That((int)MessageType.MSG_NOTIFY, Is.EqualTo(1));
        Assert.That((int)MessageType.MSG_RESPONSE, Is.EqualTo(2));
        Assert.That((int)MessageType.MSG_PUSH, Is.EqualTo(3));
    }
}
