using NUnit.Framework;
using GGFramework.GGNetwork;

public class TestHTTPRequest
{
    [Test]
    public void Test_States_EnumValues()
    {
        Assert.That((int)HTTPRequest.States.Initial, Is.EqualTo(0));
        Assert.That((int)HTTPRequest.States.Queued, Is.EqualTo(1));
        Assert.That((int)HTTPRequest.States.Processing, Is.EqualTo(2));
        Assert.That((int)HTTPRequest.States.Finished, Is.EqualTo(3));
        Assert.That((int)HTTPRequest.States.Error, Is.EqualTo(4));
        Assert.That((int)HTTPRequest.States.Aborted, Is.EqualTo(5));
        Assert.That((int)HTTPRequest.States.ConnectionTimedOut, Is.EqualTo(6));
        Assert.That((int)HTTPRequest.States.TimedOut, Is.EqualTo(7));
    }

    [Test]
    public void Test_DefaultGetState_ReturnsError()
    {
        var request = new HTTPRequest();
        Assert.That(request.GetState(), Is.EqualTo(HTTPRequest.States.Error));
    }

    [Test]
    public void Test_DefaultGetExceptionMessage_ReturnsEmpty()
    {
        var request = new HTTPRequest();
        Assert.That(request.GetExceptionMessage(), Is.EqualTo(""));
    }

    [Test]
    public void Test_DefaultGetUri_ReturnsNull()
    {
        var request = new HTTPRequest();
        Assert.That(request.GetUri(), Is.Null);
    }

    [Test]
    public void Test_DefaultGetCurrentUri_ReturnsNull()
    {
        var request = new HTTPRequest();
        Assert.That(request.GetCurrentUri(), Is.Null);
    }

    [Test]
    public void Test_CreateGetRequest_ReturnsNull()
    {
        var request = new HTTPRequest();
        var result = request.CreateGetRequest(null, HttpNetworkSystem.ExceptionAction.Ignore, null);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Test_CreatePostRequest_ReturnsNull()
    {
        var request = new HTTPRequest();
        var result = request.CreatePostRequest(null, null, null, null, HttpNetworkSystem.ExceptionAction.Ignore, null);
        Assert.That(result, Is.Null);
    }
}
