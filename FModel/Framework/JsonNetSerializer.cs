using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Serialization;

namespace FModel.Framework;

public class JsonNetSerializer : IRestSerializer
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }

    [Obsolete]
    public string Serialize(Parameter parameter)
    {
        return JsonConvert.SerializeObject(parameter.Value);
    }

    public T Deserialize<T>(IRestResponse response)
    {
        return JsonConvert.DeserializeObject<T>(response.Content, SerializerSettings);
    }

    public string[] SupportedContentTypes { get; } =
    {
        "application/json", "application/json; charset=UTF-8"
    };

    public string ContentType { get; set; } = "application/json; charset=UTF-8";

    public DataFormat DataFormat => DataFormat.Json;
}