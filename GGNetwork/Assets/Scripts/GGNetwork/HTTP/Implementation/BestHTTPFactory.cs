using System;
using GGFramework.GGNetwork;

internal class BestHTTPFactory: IHTTPFactory
{
    public HTTPRequest CreateHTTPRequest(Uri uri)
    {
        #if GGNETWORK_BESTHTTP
        HTTPRequest request = new BestHTTPRequest(uri);
        return request;
#else
        throw new NotSupportedException("Install BestHTTP and define GGNETWORK_BESTHTTP before selecting this backend.");
#endif
    }

    public HTTPResponse CreateHTTPResponse()
    {
        #if GGNETWORK_BESTHTTP
        HTTPResponse response = new BestHTTPResponse();
        return response;
#else
        throw new NotSupportedException("Install BestHTTP and define GGNETWORK_BESTHTTP before selecting this backend.");
#endif
    }

    public HTTPForm CreateHTTPForm()
    {
        HTTPForm form = new BestHTTPForm();
        return form;
    }
}
