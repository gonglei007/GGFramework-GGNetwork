using NUnit.Framework;
using GGFramework.GGNetwork;

public class TestUIAdaptor
{
    [Test]
    public void Test_GetText_NoCallback_ReturnsOriginal()
    {
        var adaptor = new UIAdaptor();
        string result = adaptor.GetText("network_error");
        Assert.That(result, Is.EqualTo("network_error"));
    }

    [Test]
    public void Test_GetText_WithCallback_TransformsText()
    {
        var adaptor = new UIAdaptor();
        adaptor.onGetText = (string text) => "LOCALIZED:" + text;
        string result = adaptor.GetText("network_error");
        Assert.That(result, Is.EqualTo("LOCALIZED:network_error"));
    }

    [Test]
    public void Test_ShowDialog_NoCallback_CallsTrue()
    {
        var adaptor = new UIAdaptor();
        bool? callbackResult = null;
        adaptor.ShowDialog("Title", "Message", true, (bool result) =>
        {
            callbackResult = result;
        });
        Assert.That(callbackResult, Is.True, "Without onDialog, callback should receive true");
    }

    [Test]
    public void Test_ShowDialog_WithCallback_Delegates()
    {
        var adaptor = new UIAdaptor();
        bool dialogShown = false;
        adaptor.onDialog = (string title, string msg, bool confirm, System.Action<bool> cb) =>
        {
            dialogShown = true;
            Assert.That(title, Is.EqualTo("Title"));
            Assert.That(msg, Is.EqualTo("Message"));
            Assert.That(confirm, Is.True);
            cb(false); // User said no
        };

        bool? userResponse = null;
        adaptor.ShowDialog("Title", "Message", true, (bool result) =>
        {
            userResponse = result;
        });

        Assert.That(dialogShown, Is.True);
        Assert.That(userResponse, Is.False);
    }

    [Test]
    public void Test_ShowDialog_WithLocalizedText()
    {
        var adaptor = new UIAdaptor();
        adaptor.onGetText = (string text) => "L:" + text;

        adaptor.onDialog = (string title, string msg, bool confirm, System.Action<bool> cb) =>
        {
            Assert.That(title, Is.EqualTo("L:server_error"));
            Assert.That(msg, Is.EqualTo("L:retry_prompt"));
            cb(true);
        };

        bool? result = null;
        adaptor.ShowDialog("server_error", "retry_prompt", false, (bool r) => { result = r; });
        Assert.That(result, Is.True);
    }

    [Test]
    public void Test_ShowWaiting_NoCallback_DoesNotThrow()
    {
        var adaptor = new UIAdaptor();
        Assert.DoesNotThrow(() => adaptor.ShowWaiting(true));
        Assert.DoesNotThrow(() => adaptor.ShowWaiting(false));
    }

    [Test]
    public void Test_ShowWaiting_WithCallback_Invokes()
    {
        var adaptor = new UIAdaptor();
        bool? waitingState = null;
        adaptor.onWaiting = (bool waiting) => { waitingState = waiting; };

        adaptor.ShowWaiting(true);
        Assert.That(waitingState, Is.True);

        adaptor.ShowWaiting(false);
        Assert.That(waitingState, Is.False);
    }
}
