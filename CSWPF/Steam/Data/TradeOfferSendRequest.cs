using System.Collections.Generic;
using CSWPF.Directory;
using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

internal sealed class TradeOfferSendRequest {
    [JsonProperty("me", Required = Required.Always)]
    internal readonly ItemList ItemsToGive = new();

    [JsonProperty("them", Required = Required.Always)]
    internal readonly ItemList ItemsToReceive = new();

    internal sealed class ItemList {
        [JsonProperty("assets", Required = Required.Always)]
        internal readonly HashSet<InventoryResponseCS.AssetCS> Assets = new();
    }
}