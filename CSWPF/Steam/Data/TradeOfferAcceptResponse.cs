using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

internal sealed class TradeOfferAcceptResponse {
    [JsonProperty("strError", Required = Required.DisallowNull)]
    internal readonly string ErrorText = "";

    [JsonProperty("needs_mobile_confirmation", Required = Required.DisallowNull)]
    internal readonly bool RequiresMobileConfirmation;

    [System.Text.Json.Serialization.JsonConstructor]
    private TradeOfferAcceptResponse() { }
}