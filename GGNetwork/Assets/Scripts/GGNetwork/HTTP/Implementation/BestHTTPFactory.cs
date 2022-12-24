using System;
using GGFramework.GGNetwork;

public class BestHTTPFactory: HTTPFactory
{
    public HTTPRequest CreateHTTPRequest(Uri uri)
    {
        BestHTTPRequest request = new BestHTTPRequest(uri);
        return request;
    }

    public HTTPResponse CreateHTTPResponse()
    {
        BestHTTPResponse response = new BestHTTPResponse();
        return response;
    }

    public HTTPForm CreateHTTPForm()
    {
        BestHTTPForm form = new BestHTTPForm();
        return form;
    }
}
