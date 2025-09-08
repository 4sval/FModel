using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace FModel.ViewModels.ApiEndpoints;

public class DynamicApiEndpoint : AbstractApiProvider
{
    public DynamicApiEndpoint(RestClient client) : base(client) { }

    public async Task<AesResponse> GetAesKeysAsync(CancellationToken token, string url, string path)
    {
        var body = await GetRequestBody(token, url).ConfigureAwait(false);
        var tokens = body.SelectTokens(path).ToArray();

        var ret = new AesResponse { MainKey = Helper.FixKey(tokens.ElementAtOrDefault(0)?.ToString()) };
        if (tokens.ElementAtOrDefault(1) is JArray dynamicKeys)
        {
            foreach (var dynamicKey in dynamicKeys)
            {
                if (dynamicKey["guid"] is not { } guid || dynamicKey["key"] is not { } key)
                    continue;

                ret.DynamicKeys.Add(new DynamicKey
                {
                    Name = dynamicKey["name"]?.ToString(),
                    Guid = guid.ToString(),
                    Key = Helper.FixKey(key.ToString())
                });
            }
        }

        return ret;
    }

    public AesResponse GetAesKeys(CancellationToken token, string url, string path)
    {
        return GetAesKeysAsync(token, url, path).GetAwaiter().GetResult();
    }

    public async Task<MappingsResponse[]> GetMappingsAsync(CancellationToken token, string url, string path, bool latest = false)
    {
        var body = await GetRequestBody(token, url).ConfigureAwait(false);
        JToken[] tokens = Array.Empty<JToken>();

        if (latest && path.Contains("LATEST"))
        {
            if (body is JObject data)
            {
                var latestVersion = data.Properties()
                    .Select(p =>
                    {
                        var key = p.Name;
                        var parts = key.Split(new[] { '_' }, 2);
                        System.Version.TryParse(parts[0], out var version);
                        return new
                        {
                            Version = version ?? new System.Version(0, 0),
                            Suffix = parts.Length > 1 ? parts[1] : string.Empty,
                            Original = key
                        };
                    })
                    .OrderByDescending(x => x.Version)
                    .ThenByDescending(x => x.Suffix)
                    .Select(x => x.Original)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(latestVersion) && data[latestVersion] is { } latestVersionObject)
                {
                    string propertySelectorPath = path.Substring(path.IndexOf("LATEST", StringComparison.Ordinal) + "LATEST".Length);
                    if (propertySelectorPath.StartsWith("."))
                    {
                        propertySelectorPath = propertySelectorPath.Substring(1);
                    }
                    tokens = latestVersionObject.SelectTokens(propertySelectorPath).ToArray();
                }
            }
            else if (body is JArray arrayData)
            {
                var latestObject = arrayData.Select(obj =>
                    {
                        var fileName = obj["fileName"]?.ToString() ?? string.Empty;
                        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d+\.\d+)");
                        return new
                        {
                            Version = match.Success ? new System.Version(match.Groups[1].Value) : new System.Version(0, 0),
                            Object = obj
                        };
                    })
                    .OrderByDescending(x => x.Version)
                    .Select(x => x.Object)
                    .FirstOrDefault();

                if (latestObject != null)
                {
                    string propertySelectorPath = path.Substring(path.IndexOf("LATEST", StringComparison.Ordinal) + "LATEST".Length);
                    if (propertySelectorPath.StartsWith("."))
                    {
                        propertySelectorPath = propertySelectorPath.Substring(1);
                    }
                    tokens = latestObject.SelectTokens(propertySelectorPath).ToArray();
                }
            }
        }

        if (tokens.Length == 0)
        {
            tokens = body.SelectTokens(path).ToArray();
        }

        var ret = new MappingsResponse[] { new() };
        ret[0].Url = tokens.ElementAtOrDefault(0)?.ToString();
        if (tokens.ElementAtOrDefault(1) is not { } fileName)
        {
            fileName = ret[0].Url?.SubstringAfterLast("/");
        }
        ret[0].FileName = fileName?.ToString();
        return ret;
    }

    public MappingsResponse[] GetMappings(CancellationToken token, string url, string path, bool latest = false)
    {
        return GetMappingsAsync(token, url, path, latest).GetAwaiter().GetResult();
    }

    public async Task<JToken> GetRequestBody(CancellationToken token, string url)
    {
        var request = new FRestRequest(url)
        {
            Interceptors = [_interceptor]
        };
        var response = await _client.ExecuteAsync(request, token).ConfigureAwait(false);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, response.ResponseUri?.OriginalString);
        return response.IsSuccessful && !string.IsNullOrEmpty(response.Content) ? JToken.Parse(response.Content) : JToken.Parse("{}");
    }
}
