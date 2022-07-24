using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Serializers;

namespace FModel.Framework;

public class JsonNetSerializer : IRestSerializer, ISerializer, IDeserializer
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public string Serialize(Parameter parameter) => JsonConvert.SerializeObject(parameter.Value);
    public string Serialize(object obj) => JsonConvert.SerializeObject(obj);
    public T Deserialize<T>(RestResponse response) => JsonConvert.DeserializeObject<T>(response.Content!, SerializerSettings);

    public ISerializer Serializer => this;
    public IDeserializer Deserializer => this;

    public string ContentType { get; set; } = "application/json";
    public string[] AcceptedContentTypes => RestSharp.Serializers.ContentType.JsonAccept;
    public SupportsContentType SupportsContentType => contentType => contentType.EndsWith("json", StringComparison.InvariantCultureIgnoreCase);

    public DataFormat DataFormat => DataFormat.Json;
}
