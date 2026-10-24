using NUnit.Framework;
using UnityEngine;
using GGFramework.GGNetwork;

/// <summary>
/// Tests for the HTTPForm wrapper around UnityEngine.WWWForm.
/// </summary>
public class TestHTTPForm
{
    [Test]
    public void Test_DefaultConstructor_CreatesForm()
    {
        var form = new HTTPForm();
        Assert.That(form.GetUnityForm(), Is.Not.Null);
    }

    [Test]
    public void Test_WrappingConstructor_ReturnsSameInstance()
    {
        var www = new WWWForm();
        www.AddField("key", "value");

        var form = new HTTPForm(www);
        Assert.That(form.GetUnityForm(), Is.SameAs(www));
    }

    [Test]
    public void Test_WrappingConstructor_Null()
    {
        var form = new HTTPForm((WWWForm)null);
        Assert.That(form.GetUnityForm(), Is.Null);
    }
}
