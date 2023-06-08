using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using CSWPF.Steam.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSWPF.Directory;

public class AssetCS
{
    public const uint SteamAppID = 730;
    public const ulong SteamCommunityContextID = 2;
    
    [System.Text.Json.Serialization.JsonIgnore]
	public IReadOnlyDictionary<string, JToken>? AdditionalPropertiesReadOnly => AdditionalProperties;

	[System.Text.Json.Serialization.JsonIgnore]
	public uint Amount { get; internal set; }

	[JsonProperty("appid", Required = Required.DisallowNull)]
	public uint AppID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong AssetID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong ClassID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong ContextID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong InstanceID { get; private set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ImmutableHashSet<Tag>? Tags { get; internal set; }

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
    
    public AssetCS(uint appID, ulong contextID, ulong classID, uint amount, ulong instanceID = 0, ulong assetID = 0, ImmutableHashSet<Tag>? tags = null) {
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
        AssetID = assetID;
        ClassID = classID;
        InstanceID = instanceID;
        Amount = amount;

        if (tags?.Count > 0) {
            Tags = tags;
        }
    }
}