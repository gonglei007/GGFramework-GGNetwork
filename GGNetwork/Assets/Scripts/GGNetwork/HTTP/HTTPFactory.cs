using System;

namespace GGFramework.GGNetwork
{
    public interface HTTPFactory
    {
        HTTPRequest CreateHTTPRequest(Uri uri);
        HTTPResponse CreateHTTPResponse();
        HTTPForm CreateHTTPForm();
    }
}
