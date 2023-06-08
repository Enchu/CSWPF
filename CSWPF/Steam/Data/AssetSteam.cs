using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using CSWPF.Steam.Data;
using CSWPF.Web;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class AssetSteam {
	public const uint SteamAppID = 753;
	public const ulong SteamCommunityContextID = 6;

	[System.Text.Json.Serialization.JsonIgnore]
	public IReadOnlyDictionary<string, JToken>? AdditionalPropertiesReadOnly => AdditionalProperties;

	[System.Text.Json.Serialization.JsonIgnore]
	public uint Amount { get; internal set; }

	[JsonProperty("appid", Required = Required.DisallowNull)]
	public uint AppID { get; private set; }
	[System.Text.Json.Serialization.JsonIgnore]
	public ulong ClassID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong AssetID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong ContextID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong InstanceID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public bool Marketable { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ERarity Rarity { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public uint RealAppID { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ImmutableHashSet<Tag>? Tags { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public bool Tradable { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public EType Type { get; internal set; }

	//[System.Text.Json.Serialization.JsonExtensionData(WriteData = false)]
	internal Dictionary<string, JToken>? AdditionalProperties { private get; set; }

	[JsonProperty("amount", Required = Required.Always)]
	private string AmountText {
		get => Amount.ToString(CultureInfo.InvariantCulture);

		set {
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			if (!uint.TryParse(value, out uint amount) || (amount == 0)) {
				return;
			}

			Amount = amount;
		}
	}

	[JsonProperty("assetid", Required = Required.DisallowNull)]
	private string AssetIDText {
		get => AssetID.ToString(CultureInfo.InvariantCulture);

		set {
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			if (!ulong.TryParse(value, out ulong assetID) || (assetID == 0)) {
				return;
			}

			AssetID = assetID;
		}
	}

	[JsonProperty("classid", Required = Required.DisallowNull)]
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

	[JsonProperty("contextid", Required = Required.DisallowNull)]
	private string ContextIDText {
		get => ContextID.ToString(CultureInfo.InvariantCulture);

		set {
			if (string.IsNullOrEmpty(value)) {
				return;
			}

			if (!ulong.TryParse(value, out ulong contextID) || (contextID == 0)) {
				return;
			}

			ContextID = contextID;
		}
	}

	[JsonProperty("id", Required = Required.DisallowNull)]
	private string IDText {
		set => AssetIDText = value;
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

	// Constructed from trades being received or plugins
	public AssetSteam(uint appID, ulong contextID, ulong classID, uint amount, ulong instanceID = 0, ulong assetID = 0, bool marketable = true, bool tradable = true, ImmutableHashSet<Tag>? tags = null, uint realAppID = 0, EType type = EType.Unknown, ERarity rarity = ERarity.Unknown) {
		if (appID == 0) {
			throw new ArgumentOutOfRangeException(nameof(appID));
		}

		if (contextID == 0) {
			throw new ArgumentOutOfRangeException(nameof(contextID));
		}

		if (classID == 0) {
			throw new ArgumentOutOfRangeException(nameof(classID));
		}

		if (amount == 0) {
			throw new ArgumentOutOfRangeException(nameof(amount));
		}

		AppID = appID;
		ContextID = contextID;
		ClassID = classID;
		Amount = amount;
		InstanceID = instanceID;
		AssetID = assetID;
		Marketable = marketable;
		Tradable = tradable;
		RealAppID = realAppID;
		Type = type;
		Rarity = rarity;

		if (tags?.Count > 0) {
			Tags = tags;
		}
	}

	//todo: Null
	[System.Text.Json.Serialization.JsonConstructor]
	private AssetSteam() { }

	internal AssetSteam CreateShallowCopy() => (AssetSteam) MemberwiseClone();

	public enum ERarity : byte {
		Unknown,
		Common,
		Uncommon,
		Rare
	}

	public enum EType : byte {
		Unknown,
		BoosterPack,
		Emoticon,
		FoilTradingCard,
		ProfileBackground,
		TradingCard,
		SteamGems,
		SaleItem,
		Consumable,
		ProfileModifier,
		Sticker,
		ChatEffect,
		MiniProfileBackground,
		AvatarProfileFrame,
		AnimatedAvatar,
		KeyboardSkin
	}
}