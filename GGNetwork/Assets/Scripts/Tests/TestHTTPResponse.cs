using NUnit.Framework;
using GGFramework.GGNetwork;

public class TestHTTPResponse
{
    [Test]
    public void Test_DefaultIsSuccess_False()
    {
        var response = new HTTPResponse();
        Assert.That(response.IsSuccess(), Is.False);
    }

    [Test]
    public void Test_DefaultStatusCode_IsZero()
    {
        var response = new HTTPResponse();
        Assert.That(response.GetStatusCode(), Is.EqualTo(0));
    }

    [Test]
    public void Test_DefaultGetData_IsNull()
    {
        var response = new HTTPResponse();
        Assert.That(response.GetData(), Is.Null);
    }

    [Test]
    public void Test_DefaultGetMessage_IsNull()
    {
        var response = new HTTPResponse();
        Assert.That(response.GetMessage(), Is.Null);
    }
}
