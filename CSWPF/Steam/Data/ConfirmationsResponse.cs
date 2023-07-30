using System.Collections.Immutable;
using CSWPF.Steam.Security;
using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

internal sealed class ConfirmationsResponse : BooleanResponse {
    [JsonProperty("conf", Required = Required.Always)]
    internal readonly ImmutableHashSet<Confirmation> Confirmations = ImmutableHashSet<Confirmation>.Empty;

    [System.Text.Json.Serialization.JsonConstructor]
    private ConfirmationsResponse() { }
}