using NUnit.Framework;
using GGFramework.GGNetwork;

public class TestNetworkConst
{
    [Test]
    public void Test_CodeConstants()
    {
        Assert.That(NetworkConst.CODE_OK, Is.EqualTo(200));
        Assert.That(NetworkConst.CODE_FAILED, Is.EqualTo(500));
        Assert.That(NetworkConst.CODE_FA_TIMEOUT, Is.EqualTo(501));
        Assert.That(NetworkConst.CODE_Silent, Is.EqualTo(600));
        Assert.That(NetworkConst.CODE_FA_REPEAT_MSG, Is.EqualTo(2007));
        Assert.That(NetworkConst.CODE_RESPONSE_MSG_ERROR, Is.EqualTo(3001));
    }

    [Test]
    public void Test_ErrorMessage()
    {
        Assert.That(NetworkConst.ERROR_MESSAGE, Is.EqualTo("network_error"));
    }

    [Test]
    public void Test_InitEx_Defaults()
    {
        // InitEx with no params should set null/default values
        NetworkConst.InitEx();
        Assert.That(NetworkConst.httpSecretKey, Is.Null);
        Assert.That(NetworkConst.deviceUID, Is.Null);
        Assert.That(NetworkConst.channel, Is.Null);
        Assert.That(NetworkConst.clientVersion, Is.Null);
    }

    [Test]
    public void Test_InitEx_WithParams()
    {
        NetworkConst.InitEx(
            secretKey: "test-secret-key-123",
            deviceUID: "device-001",
            channel: "AppStore",
            clientVersion: "1.2.3"
        );

        Assert.That(NetworkConst.httpSecretKey, Is.EqualTo("test-secret-key-123"));
        Assert.That(NetworkConst.deviceUID, Is.EqualTo("device-001"));
        Assert.That(NetworkConst.channel, Is.EqualTo("AppStore"));
        Assert.That(NetworkConst.clientVersion, Is.EqualTo("1.2.3"));
    }
}
