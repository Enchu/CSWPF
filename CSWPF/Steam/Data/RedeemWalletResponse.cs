using Newtonsoft.Json;
using SteamKit2;

namespace CSWPF.Steam.Data;

internal sealed class RedeemWalletResponse : ResultResponse {
    [JsonProperty("formattednewwalletbalance", Required = Required.DisallowNull)]
    internal readonly string? BalanceText;

    [JsonProperty("detail", Required = Required.DisallowNull)]
    internal readonly EPurchaseResultDetail PurchaseResultDetail;

    [JsonConstructor]
    private RedeemWalletResponse() { }
}