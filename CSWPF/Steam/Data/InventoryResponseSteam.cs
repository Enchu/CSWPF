using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace CSWPF.Steam.Data;

internal sealed class InventoryResponseSteam : OptionalResultResponse {
	[JsonProperty("assets", Required = Required.DisallowNull)]
	internal readonly ImmutableList<AssetSteam> Assets = ImmutableList<AssetSteam>.Empty;

	[JsonProperty("descriptions", Required = Required.DisallowNull)]
	internal readonly ImmutableHashSet<Description> Descriptions = ImmutableHashSet<Description>.Empty;

	[JsonProperty("total_inventory_count", Required = Required.DisallowNull)]
	internal readonly uint TotalInventoryCount;

	internal EResult? ErrorCode { get; private set; }
	internal string? ErrorText { get; private set; }
	internal ulong LastAssetID { get; private set; }
	internal bool MoreItems { get; private set; }

	[JsonProperty("error", Required = Required.DisallowNull)]
	private string Error {
		set {
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			//ErrorCode = SteamUtilities.InterpretError(value);
			ErrorText = value;
		}
	}

	[JsonProperty("last_assetid", Required = Required.DisallowNull)]
	private string LastAssetIDText {
		set {
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			if (!ulong.TryParse(value, out ulong lastAssetID) || (lastAssetID == 0)) {
				return;
			}

			LastAssetID = lastAssetID;
		}
	}

	[JsonProperty("more_items", Required = Required.DisallowNull)]
	private byte MoreItemsNumber {
		set => MoreItems = value > 0;
	}

	[JsonConstructor]
	private InventoryResponseSteam() { }

	internal sealed class Description {
		internal AssetSteam.ERarity Rarity {
			get {
				foreach (Tag tag in Tags) {
					switch (tag.Identifier) {
						case "droprate":
							switch (tag.Value) {
								case "droprate_0":
									return AssetSteam.ERarity.Common;
								case "droprate_1":
									return AssetSteam.ERarity.Uncommon;
								case "droprate_2":
									return AssetSteam.ERarity.Rare;
								default:
									break;
							}

							break;
					}
				}

				return AssetSteam.ERarity.Unknown;
			}
		}

		internal uint RealAppID {
			get {
				foreach (Tag tag in Tags) {
					switch (tag.Identifier) {
						case "Game":
							if (string.IsNullOrEmpty(tag.Value) || (tag.Value.Length <= 4) || !tag.Value.StartsWith("app_", StringComparison.Ordinal)) {
								break;
							}

							string appIDText = tag.Value[4..];

							if (!uint.TryParse(appIDText, out uint appID) || (appID == 0)) {
								break;
							}

							return appID;
					}
				}

				return 0;
			}
		}

		internal AssetSteam.EType Type {
			get {
				AssetSteam.EType type = AssetSteam.EType.Unknown;

				foreach (Tag tag in Tags) {
					switch (tag.Identifier) {
						case "cardborder":
							switch (tag.Value) {
								case "cardborder_0":
									return AssetSteam.EType.TradingCard;
								case "cardborder_1":
									return AssetSteam.EType.FoilTradingCard;
								default:
									return AssetSteam.EType.Unknown;
							}
						case "item_class":
							switch (tag.Value) {
								case "item_class_2":
									if (type == AssetSteam.EType.Unknown) {
										// This is a fallback in case we'd have no cardborder available to interpret
										type = AssetSteam.EType.TradingCard;
									}

									continue;
								case "item_class_3":
									return AssetSteam.EType.ProfileBackground;
								case "item_class_4":
									return AssetSteam.EType.Emoticon;
								case "item_class_5":
									return AssetSteam.EType.BoosterPack;
								case "item_class_6":
									return AssetSteam.EType.Consumable;
								case "item_class_7":
									return AssetSteam.EType.SteamGems;
								case "item_class_8":
									return AssetSteam.EType.ProfileModifier;
								case "item_class_10":
									return AssetSteam.EType.SaleItem;
								case "item_class_11":
									return AssetSteam.EType.Sticker;
								case "item_class_12":
									return AssetSteam.EType.ChatEffect;
								case "item_class_13":
									return AssetSteam.EType.MiniProfileBackground;
								case "item_class_14":
									return AssetSteam.EType.AvatarProfileFrame;
								case "item_class_15":
									return AssetSteam.EType.AnimatedAvatar;
								case "item_class_16":
									return AssetSteam.EType.KeyboardSkin;
								default:
									return AssetSteam.EType.Unknown;
							}
					}
				}

				return type;
			}
		}

		[JsonExtensionData(WriteData = false)]
		internal Dictionary<string, JToken>? AdditionalProperties {
			get;
			[UsedImplicitly]
			set;
		}

		[JsonProperty("appid", Required = Required.Always)]
		internal uint AppID { get; set; }

		internal ulong ClassID { get; set; }
		internal ulong InstanceID { get; set; }
		internal bool Marketable { get; set; }

		[JsonProperty("tags", Required = Required.DisallowNull)]
		internal ImmutableHashSet<Tag> Tags { get; set; } = ImmutableHashSet<Tag>.Empty;

		internal bool Tradable { get; set; }

		[JsonProperty("classid", Required = Required.Always)]
		private string ClassIDText {
			set {
				if (string.IsNullOrEmpty(value)) {
					return;
				}

				if (!ulong.TryParse(value, out ulong classID) || (classID == 0)) {
					return;
				}

				ClassID = classID;
			}
		}

		[JsonProperty("instanceid", Required = Required.DisallowNull)]
		private string InstanceIDText {
			set {
				if (string.IsNullOrEmpty(value)) {
					return;
				}

				if (!ulong.TryParse(value, out ulong instanceID)) {
					return;
				}

				InstanceID = instanceID;
			}
		}

		[JsonProperty("marketable", Required = Required.Always)]
		private byte MarketableNumber {
			set => Marketable = value > 0;
		}

		[JsonProperty("tradable", Required = Required.Always)]
		private byte TradableNumber {
			set => Tradable = value > 0;
		}

		[JsonConstructor]
		internal Description() { }
	}
}
