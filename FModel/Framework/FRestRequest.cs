using System;
using FModel.Settings;
using RestSharp;

namespace FModel.Framework;

public class FRestRequest : RestRequest
{
    private int TimeoutSeconds = UserSettings.Default.HttpRequestTimeout;

    public FRestRequest(string url, Method method = Method.Get) : base(url, method)
    {
        Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }

    public FRestRequest(Uri uri, Method method = Method.Get) : base(uri, method)
    {
        Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }
}
