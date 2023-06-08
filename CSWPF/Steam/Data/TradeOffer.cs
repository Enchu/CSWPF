using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SteamKit2;

namespace CSWPF.Steam.Data;

public sealed class TradeOffer {
    public IReadOnlyCollection<AssetSteam> ItemsToGiveReadOnly => ItemsToGive;
    public IReadOnlyCollection<AssetSteam> ItemsToReceiveReadOnly => ItemsToReceive;
    internal readonly HashSet<AssetSteam> ItemsToGive = new();
    internal readonly HashSet<AssetSteam> ItemsToReceive = new();

    public ulong OtherSteamID64 { get; private set; }

    public ETradeOfferState State { get; private set; }

    public ulong TradeOfferID { get; private set; }

    // Constructed from trades being received
    internal TradeOffer(ulong tradeOfferID, uint otherSteamID3, ETradeOfferState state) {
        if (tradeOfferID == 0) {
            throw new ArgumentOutOfRangeException(nameof(tradeOfferID));
        }

        if (otherSteamID3 == 0) {
            throw new ArgumentOutOfRangeException(nameof(otherSteamID3));
        }

        if (!Enum.IsDefined(state)) {
            throw new InvalidEnumArgumentException(nameof(state), (int) state, typeof(ETradeOfferState));
        }

        TradeOfferID = tradeOfferID;
        OtherSteamID64 = new SteamID(otherSteamID3, EUniverse.Public, EAccountType.Individual);
        State = state;
    }

    public bool IsValidSteamItemsRequest(IReadOnlyCollection<AssetSteam.EType> acceptedTypes) {
        if ((acceptedTypes == null) || (acceptedTypes.Count == 0)) {
            throw new ArgumentNullException(nameof(acceptedTypes));
        }

        return ItemsToGive.All(item => item is { AppID: AssetSteam.SteamAppID, ContextID: AssetSteam.SteamCommunityContextID } && acceptedTypes.Contains(item.Type));
    }
}