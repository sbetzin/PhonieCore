using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PhonieCore.Mopidy.Model
{
    public class Request
    {
        [JsonProperty("jsonrpc")] public string Jsonrpc { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("method")] public string Method { get; set; }

        [JsonProperty("params")] public Dictionary<string, object> Params { get; set; }

    }
}
