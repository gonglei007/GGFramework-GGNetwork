using NUnit.Framework;
using SimpleJson;
using GGFramework.GGNetwork;

public class TestLogAdaptor
{
    [Test]
    public void Test_PostError_String_NoCallback_DoesNotThrow()
    {
        var adaptor = new LogAdaptor();
        Assert.DoesNotThrow(() => adaptor.PostError("ERR001", "Something went wrong"));
    }

    [Test]
    public void Test_PostError_String_WithCallback()
    {
        var adaptor = new LogAdaptor();
        string receivedId = null;
        string receivedInfo = null;

        adaptor.onPostError = (string id, string info) =>
        {
            receivedId = id;
            receivedInfo = info;
        };

        adaptor.PostError("ERR_TIMEOUT", "Connection timed out after 15s");

        Assert.That(receivedId, Is.EqualTo("ERR_TIMEOUT"));
        Assert.That(receivedInfo, Is.EqualTo("Connection timed out after 15s"));
    }

    [Test]
    public void Test_PostError_Network_NoCallback_DoesNotThrow()
    {
        var adaptor = new LogAdaptor();
        JsonObject param = new JsonObject();
        param["host"] = "api.example.com";
        param["error"] = "timeout";
        Assert.DoesNotThrow(() => adaptor.PostError("api.example.com", param));
    }

    [Test]
    public void Test_PostError_Network_WithCallback()
    {
        var adaptor = new LogAdaptor();
        string receivedHost = null;
        JsonObject receivedParam = null;

        adaptor.onPostNetworkError = (string host, JsonObject param) =>
        {
            receivedHost = host;
            receivedParam = param;
        };

        JsonObject errorParam = new JsonObject();
        errorParam["statusCode"] = 500;
        errorParam["message"] = "Internal Server Error";

        adaptor.PostError("game-server.example.com", errorParam);

        Assert.That(receivedHost, Is.EqualTo("game-server.example.com"));
        Assert.That(receivedParam["statusCode"], Is.EqualTo(500));
        Assert.That(receivedParam["message"], Is.EqualTo("Internal Server Error"));
    }

    [Test]
    public void Test_PostError_MultipleCallbacks_LastWins()
    {
        var adaptor = new LogAdaptor();
        int callCount = 0;

        // First callback
        adaptor.onPostError = (string id, string info) => { callCount++; };
        // Second callback (overwrites first)
        adaptor.onPostError = (string id, string info) => { callCount += 10; };

        adaptor.PostError("ERR", "test");

        Assert.That(callCount, Is.EqualTo(10), "Only the last callback should be invoked");
    }
}
