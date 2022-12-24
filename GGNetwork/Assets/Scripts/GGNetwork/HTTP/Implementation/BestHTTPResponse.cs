using System;
using GGFramework.GGNetwork;

public class BestHTTPResponse: HTTPResponse
{
    private BestHTTP.HTTPResponse response;

    public BestHTTPResponse()
    {
    }

    public BestHTTPResponse(BestHTTP.HTTPResponse response)
    {
        this.response = response;
    }

    public override bool IsSuccess()
    {
        return response.IsSuccess;
    }

    public override int GetStatusCode()
    {
        return response.StatusCode;
    }

    public override string GetData()
    {
        return response.DataAsText;
    }

    public override string GetMessage()
    {
        return response.Message;
    }
}
