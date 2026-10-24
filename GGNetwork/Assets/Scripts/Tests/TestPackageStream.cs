using System.Collections.Generic;
using NUnit.Framework;
using GGFramework.GGNetwork.Socket;

/// <summary>
/// Tests for PackageStream, which reassembles Pomelo frames from arbitrary
/// WebSocket byte chunks (handles both sticky packets and fragmentation).
/// </summary>
public class TestPackageStream
{
    private List<Package> received;
    private PackageStream stream;

    [SetUp]
    public void SetUp()
    {
        received = new List<Package>();
        stream = new PackageStream(pkg => received.Add(pkg));
    }

    [TearDown]
    public void TearDown()
    {
        stream?.Dispose();
    }

    [Test]
    public void Test_Feed_SingleCompletePackage()
    {
        byte[] body = new byte[] { 1, 2, 3 };
        stream.Feed(PackageProtocol.Encode(PackageType.PKG_DATA, body));

        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].type, Is.EqualTo(PackageType.PKG_DATA));
        Assert.That(received[0].body, Is.EqualTo(body));
    }

    [Test]
    public void Test_Feed_HeartbeatNoBody()
    {
        stream.Feed(PackageProtocol.Encode(PackageType.PKG_HEARTBEAT));

        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].type, Is.EqualTo(PackageType.PKG_HEARTBEAT));
        Assert.That(received[0].body.Length, Is.EqualTo(0));
    }

    [Test]
    public void Test_Feed_StickyPackets_TwoAtOnce()
    {
        byte[] p1 = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 0xA });
        byte[] p2 = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 0xB, 0xC });

        byte[] combined = new byte[p1.Length + p2.Length];
        System.Array.Copy(p1, 0, combined, 0, p1.Length);
        System.Array.Copy(p2, 0, combined, p1.Length, p2.Length);

        stream.Feed(combined);

        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received[0].body, Is.EqualTo(new byte[] { 0xA }));
        Assert.That(received[1].body, Is.EqualTo(new byte[] { 0xB, 0xC }));
    }

    [Test]
    public void Test_Feed_FragmentedPackage()
    {
        byte[] full = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 1, 2, 3, 4, 5 });

        // Split into an incomplete header chunk, then the rest.
        stream.Feed(new byte[] { full[0], full[1] });
        Assert.That(received.Count, Is.EqualTo(0), "Incomplete header must not emit a package");

        byte[] remainder = new byte[full.Length - 2];
        System.Array.Copy(full, 2, remainder, 0, remainder.Length);
        stream.Feed(remainder);

        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].body, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Test_Feed_ByteByByte()
    {
        byte[] full = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 9, 8, 7 });
        for (int i = 0; i < full.Length - 1; i++)
        {
            stream.Feed(new byte[] { full[i] });
            Assert.That(received.Count, Is.EqualTo(0));
        }
        stream.Feed(new byte[] { full[full.Length - 1] });
        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].body, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    [Test]
    public void Test_Feed_MultiplePackagesThenFragment()
    {
        byte[] p1 = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 1 });
        byte[] p2 = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 2, 2 });

        // First feed delivers p1 + first half of p2.
        byte[] mixed = new byte[p1.Length + 3];
        System.Array.Copy(p1, 0, mixed, 0, p1.Length);
        System.Array.Copy(p2, 0, mixed, p1.Length, 3);
        stream.Feed(mixed);

        Assert.That(received.Count, Is.EqualTo(1), "Only p1 is complete after first feed");

        // Deliver the remaining bytes of p2.
        byte[] tail = new byte[p2.Length - 3];
        System.Array.Copy(p2, 3, tail, 0, tail.Length);
        stream.Feed(tail);

        Assert.That(received.Count, Is.EqualTo(2));
        Assert.That(received[1].body, Is.EqualTo(new byte[] { 2, 2 }));
    }

    [Test]
    public void Test_Feed_NullOrEmpty_NoCallback()
    {
        Assert.DoesNotThrow(() => stream.Feed(null));
        Assert.DoesNotThrow(() => stream.Feed(new byte[0]));
        Assert.That(received.Count, Is.EqualTo(0));
    }

    [Test]
    public void Test_Feed_IllegalLength_DiscardsBufferNoHang()
    {
        // Declare a body length of 0xFFFFFF (max) which overflows the allowed total.
        byte[] illegal = new byte[] { (byte)PackageType.PKG_DATA, 0xFF, 0xFF, 0xFF };
        Assert.DoesNotThrow(() => stream.Feed(illegal));
        Assert.That(received.Count, Is.EqualTo(0));

        // Buffer should have been cleared; a fresh valid package still works.
        stream.Feed(PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 5 }));
        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].body, Is.EqualTo(new byte[] { 5 }));
    }

    [Test]
    public void Test_Clear_DropsPendingBytes()
    {
        byte[] full = PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 1, 2, 3 });
        stream.Feed(new byte[] { full[0], full[1], full[2] }); // partial
        stream.Clear();
        stream.Feed(full); // feeding a full packet now should emit exactly one

        Assert.That(received.Count, Is.EqualTo(1));
        Assert.That(received[0].body, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void Test_Dispose_StopsCallbacks()
    {
        stream.Dispose();
        stream.Feed(PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 1 }));
        Assert.That(received.Count, Is.EqualTo(0));
    }

    [Test]
    public void Test_Constructor_NullCallback_DoesNotThrow()
    {
        var nullStream = new PackageStream(null);
        Assert.DoesNotThrow(() => nullStream.Feed(PackageProtocol.Encode(PackageType.PKG_DATA, new byte[] { 1 })));
        nullStream.Dispose();
    }
}
