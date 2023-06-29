using CSWPF.Utils;
using Newtonsoft.Json;
using SteamKit2;

namespace CSWPF.Steam.Data;

internal sealed class TradeOfferSendResponse {
    [JsonProperty("strError", Required = Required.DisallowNull)]
    internal readonly string ErrorText = "";

    [JsonProperty("needs_mobile_confirmation", Required = Required.DisallowNull)]
    internal readonly bool RequiresMobileConfirmation;

    internal ulong TradeOfferID { get; private set; }

    [JsonProperty("tradeofferid", Required = Required.DisallowNull)]
    private string TradeOfferIDText {
        set {
            if (string.IsNullOrEmpty(value)) {
                return;
            }

            if (!ulong.TryParse(value, out ulong tradeOfferID) || (tradeOfferID == 0)) {
                return;
            }

            TradeOfferID = tradeOfferID;
        }
    }

    [JsonConstructor]
    private TradeOfferSendResponse() { }
}