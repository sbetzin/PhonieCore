using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhonieCore.Mopidy.Model;

public class WebSocketResponse
{
    [JsonProperty("jsonrpc")]
    public string Jsonrpc { get; set; }

    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("result")]
    public object Result { get; set; }

    [JsonProperty("error")]
    public ErrorResponse Error { get; set; }

    [JsonProperty("event")]
    public string Event { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; }
}