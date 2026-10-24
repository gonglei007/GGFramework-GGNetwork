using NUnit.Framework;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for the Pomelo varint codec helpers (WsEncoder / WsDecoder).
/// These are pure functions used by MessageProtocol for request-id encoding.
/// </summary>
public class TestWsCodec
{
    // ---------- WsEncoder.EncodeUInt32 ----------

    [Test]
    public void Test_EncodeUInt32_Zero()
    {
        byte[] bytes = WsEncoder.EncodeUInt32(0);
        Assert.That(bytes, Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void Test_EncodeUInt32_SingleByte()
    {
        // 127 fits in one 7-bit group, no continuation bit.
        byte[] bytes = WsEncoder.EncodeUInt32(127);
        Assert.That(bytes, Is.EqualTo(new byte[] { 127 }));
    }

    [Test]
    public void Test_EncodeUInt32_TwoBytes()
    {
        // 300 => low 7 bits (44) with continuation, then 2.
        byte[] bytes = WsEncoder.EncodeUInt32(300);
        Assert.That(bytes, Is.EqualTo(new byte[] { 172, 2 }));
    }

    [Test]
    public void Test_EncodeUInt32_128()
    {
        // 128 => 0 with continuation + 1
        byte[] bytes = WsEncoder.EncodeUInt32(128);
        Assert.That(bytes, Is.EqualTo(new byte[] { 128, 1 }));
    }

    // ---------- Round trips ----------

    [TestCase(0u)]
    [TestCase(1u)]
    [TestCase(127u)]
    [TestCase(128u)]
    [TestCase(300u)]
    [TestCase(16383u)]
    [TestCase(16384u)]
    [TestCase(2097151u)]
    [TestCase(uint.MaxValue)]
    public void Test_UInt32_RoundTrip(uint value)
    {
        byte[] encoded = WsEncoder.EncodeUInt32(value);
        int length;
        uint decoded = WsDecoder.DecodeUInt32(0, encoded, out length);
        Assert.That(decoded, Is.EqualTo(value));
        Assert.That(length, Is.EqualTo(encoded.Length));
    }

    [Test]
    public void Test_DecodeUInt32_WithOffset()
    {
        byte[] valueBytes = WsEncoder.EncodeUInt32(300);
        byte[] buffer = new byte[valueBytes.Length + 3];
        buffer[0] = 0xFF; // noise before
        System.Array.Copy(valueBytes, 0, buffer, 3, valueBytes.Length);

        int length;
        uint decoded = WsDecoder.DecodeUInt32(3, buffer, out length);
        Assert.That(decoded, Is.EqualTo(300u));
        Assert.That(length, Is.EqualTo(valueBytes.Length));
    }

    [Test]
    public void Test_DecodeUInt32_StopsAtContinuationBit()
    {
        // A single-byte value followed by trailing garbage must decode only the first byte.
        byte[] buffer = new byte[] { 44, 200, 200 };
        int length;
        uint decoded = WsDecoder.DecodeUInt32(0, buffer, out length);
        Assert.That(decoded, Is.EqualTo(44u));
        Assert.That(length, Is.EqualTo(1));
    }

    // ---------- SInt32 (ZigZag) ----------

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(-2)]
    [TestCase(100)]
    [TestCase(-100)]
    public void Test_SInt32_RoundTrip(int value)
    {
        byte[] encoded = WsEncoder.EncodeSInt32(value);
        int decoded = WsDecoder.DecodeSInt32(encoded);
        Assert.That(decoded, Is.EqualTo(value));
    }

    // ---------- ByteLength ----------

    [Test]
    public void Test_ByteLength_Ascii()
    {
        Assert.That(WsEncoder.ByteLength("route"), Is.EqualTo(5));
    }

    [Test]
    public void Test_ByteLength_MultiByteUtf8()
    {
        // Each CJK char below is 3 bytes in UTF-8.
        Assert.That(WsEncoder.ByteLength("游戏"), Is.EqualTo(6));
    }

    [Test]
    public void Test_ByteLength_Empty()
    {
        Assert.That(WsEncoder.ByteLength(string.Empty), Is.EqualTo(0));
    }
}
