using System;
using GGFramework.GGNetwork;

internal class BestHTTPFactory: HTTPFactory
{
    public HTTPRequest CreateHTTPRequest(Uri uri)
    {
        HTTPRequest request = new BestHTTPRequest(uri);
        return request;
    }

    public HTTPResponse CreateHTTPResponse()
    {
        HTTPResponse response = new BestHTTPResponse();
        return response;
    }

    public HTTPForm CreateHTTPForm()
    {
        HTTPForm form = new BestHTTPForm();
        return form;
    }
}
