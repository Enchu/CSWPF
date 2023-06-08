using Newtonsoft.Json;
using SteamKit2;

namespace CSWPF.Steam.Data;

public class OptionalResultResponse {
    [JsonProperty("success", Required = Required.DisallowNull)]
    public EResult? Result { get; private set; }

    [JsonConstructor]
    protected OptionalResultResponse() { }
}