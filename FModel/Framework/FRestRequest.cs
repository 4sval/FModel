using System;
using RestSharp;

namespace FModel.Framework;

public class FRestRequest : RestRequest
{
    private const int _timeout = 3 * 1000;

    public FRestRequest(string url, Method method = Method.Get) : base(url, method)
    {
        Timeout = _timeout;
    }

    public FRestRequest(Uri uri, Method method = Method.Get) : base(uri, method)
    {
        Timeout = _timeout;
    }
}
