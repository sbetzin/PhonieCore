﻿using Newtonsoft.Json;

namespace PhonieCore.Mopidy.Model;

public class ErrorResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }
}
