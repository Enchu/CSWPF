using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

internal sealed class AccessTokenResponse : ResultResponse {
    [JsonProperty("data", Required = Required.Always)]
    internal readonly AccessTokenData Data = new();

    [System.Text.Json.Serialization.JsonConstructor]
    private AccessTokenResponse() { }

    [SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
    internal sealed class AccessTokenData {
        [JsonProperty("webapi_token", Required = Required.Always)]
        internal readonly string WebAPIToken = "";

        [System.Text.Json.Serialization.JsonConstructor]
        internal AccessTokenData() { }
    }
}