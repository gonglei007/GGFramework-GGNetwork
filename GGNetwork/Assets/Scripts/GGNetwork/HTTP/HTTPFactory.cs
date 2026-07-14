using System;

namespace GGFramework.GGNetwork
{
    public interface IHTTPFactory
    {
        HTTPRequest CreateHTTPRequest(Uri uri);
        HTTPResponse CreateHTTPResponse();
        HTTPForm CreateHTTPForm();
    }
}
