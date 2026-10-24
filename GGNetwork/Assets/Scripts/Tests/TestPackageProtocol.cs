using System;
using NUnit.Framework;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for the Pomelo transport frame codec ([type(1)][len(3)][body])
/// and the Package data holder.
/// </summary>
public class TestPackageProtocol
{
    // ---------- Package ----------

    [Test]
    public void Test_Package_Constructor_SetsLengthFromBody()
    {
        byte[] body = new byte[] { 1, 2, 3 };
        var pkg = new Package(PackageType.PKG_DATA, body);
        Assert.That(pkg.type, Is.EqualTo(PackageType.PKG_DATA));
        Assert.That(pkg.length, Is.EqualTo(3));
        Assert.That(pkg.body, Is.EqualTo(body));
    }

    [Test]
    public void Test_Package_Constructor_NullBody_LengthZero()
    {
        var pkg = new Package(PackageType.PKG_HEARTBEAT, null);
        Assert.That(pkg.length, Is.EqualTo(0));
        Assert.That(pkg.body, Is.Null);
    }

    [Test]
    public void Test_PackageType_Values()
    {
        Assert.That((int)PackageType.PKG_HANDSHAKE, Is.EqualTo(1));
        Assert.That((int)PackageType.PKG_HANDSHAKE_ACK, Is.EqualTo(2));
        Assert.That((int)PackageType.PKG_HEARTBEAT, Is.EqualTo(3));
        Assert.That((int)PackageType.PKG_DATA, Is.EqualTo(4));
        Assert.That((int)PackageType.PKG_KICK, Is.EqualTo(5));
    }

    // ---------- Encode (empty) ----------

    [Test]
    public void Test_Encode_TypeOnly_ProducesFourZeroLengthBytes()
    {
        byte[] buf = PackageProtocol.Encode(PackageType.PKG_HEARTBEAT);
        Assert.That(buf, Is.EqualTo(new byte[] { 3, 0, 0, 0 }));
    }

    // ---------- Encode (with body) ----------

    [Test]
    public void Test_Encode_WithBody_WritesBigEndianLength()
    {
        byte[] body = new byte[] { 0xAA, 0xBB, 0xCC };
        byte[] buf = PackageProtocol.Encode(PackageType.PKG_DATA, body);

        Assert.That(buf.Length, Is.EqualTo(PackageProtocol.HEADER_LENGTH + body.Length));
        Assert.That(buf[0], Is.EqualTo((byte)PackageType.PKG_DATA));
        // 3-byte big-endian length = 3
        Assert.That(buf[1], Is.EqualTo(0));
        Assert.That(buf[2], Is.EqualTo(0));
        Assert.That(buf[3], Is.EqualTo(3));
        Assert.That(buf[4], Is.EqualTo(0xAA));
        Assert.That(buf[5], Is.EqualTo(0xBB));
        Assert.That(buf[6], Is.EqualTo(0xCC));
    }

    [Test]
    public void Test_Encode_WithEmptyBody_HeaderOnly()
    {
        byte[] buf = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[0]);
        Assert.That(buf, Is.EqualTo(new byte[] { 4, 0, 0, 0 }));
    }

    [Test]
    public void Test_Encode_WithNullBody_DoesNotThrow()
    {
        // Regression guard: previously dereferenced body.Length and threw NRE.
        byte[] buf = null;
        Assert.DoesNotThrow(() => buf = PackageProtocol.Encode(PackageType.PKG_DATA, null));
        Assert.That(buf, Is.EqualTo(new byte[] { 4, 0, 0, 0 }));
    }

    [Test]
    public void Test_Encode_LargeBody_ThreeByteLength()
    {
        byte[] body = new byte[0x010203]; // 66051, exercises all three length bytes
        byte[] buf = PackageProtocol.Encode(PackageType.PKG_DATA, body);
        Assert.That(buf[1], Is.EqualTo(0x01));
        Assert.That(buf[2], Is.EqualTo(0x02));
        Assert.That(buf[3], Is.EqualTo(0x03));
        Assert.That(buf.Length, Is.EqualTo(4 + 0x010203));
    }

    // ---------- Decode ----------

    [Test]
    public void Test_Decode_RoundTrip()
    {
        byte[] body = new byte[] { 10, 20, 30, 40 };
        byte[] buf = PackageProtocol.Encode(PackageType.PKG_DATA, body);

        Package pkg = PackageProtocol.Decode(buf);
        Assert.That(pkg.type, Is.EqualTo(PackageType.PKG_DATA));
        Assert.That(pkg.body, Is.EqualTo(body));
        Assert.That(pkg.length, Is.EqualTo(body.Length));
    }

    [Test]
    public void Test_Decode_NullBuffer_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageProtocol.Decode(null));
    }

    [Test]
    public void Test_Decode_TooShortBuffer_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageProtocol.Decode(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Test_Decode_ExactlyHeader_ReturnsEmptyBody()
    {
        Package pkg = PackageProtocol.Decode(new byte[] { 3, 0, 0, 0 });
        Assert.That(pkg.type, Is.EqualTo(PackageType.PKG_HEARTBEAT));
        Assert.That(pkg.body.Length, Is.EqualTo(0));
    }
}
