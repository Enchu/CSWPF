using Newtonsoft.Json;
using SteamKit2;

namespace CSWPF.Steam.Data;

public class ResultResponse {
    [JsonProperty("success", Required = Required.Always)]
    public EResult Result { get; private set; }

    [JsonConstructor]
    protected ResultResponse() { }
}