using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

public class BooleanResponse {
    // You say it works in a RESTFUL way
    // Then your errors come back as 200 OK
    [JsonProperty("success", Required = Required.Always)]
    public bool Success { get; private set; }

    [System.Text.Json.Serialization.JsonConstructor]
    protected BooleanResponse() { }
}