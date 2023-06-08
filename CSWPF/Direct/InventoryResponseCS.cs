using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CSWPF.Steam.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace CSWPF.Directory;

public class InventoryResponseCS: OptionalResultResponse {
	
	[JsonProperty("assets", Required = Required.DisallowNull)]
	internal readonly ImmutableList<AssetCS> Assets = ImmutableList<AssetCS>.Empty; 
	[JsonProperty("descriptions", Required = Required.DisallowNull)]
	internal readonly ImmutableHashSet<Description> Descriptions = ImmutableHashSet<Description>.Empty;

	[JsonProperty("total_inventory_count", Required = Required.DisallowNull)]
	internal readonly uint TotalInventoryCount;

	internal EResult? ErrorCode { get; private set; }
	internal string? ErrorText { get; private set; }
	internal ulong LastAssetID { get; private set; }
	internal bool MoreItems { get; private set; }

	[JsonProperty("error", Required = Required.DisallowNull)]
	private string Error
	{
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				return;
			}

			//ErrorCode = SteamUtilities.InterpretError(value);
			ErrorText = value;
		}
	}

	[JsonConstructor]
	private InventoryResponseCS()
	{
	}

	internal sealed class Description
	{
		internal uint RealAppID
		{
			get
			{
				foreach (Tag tag in Tags)
				{
					switch (tag.Identifier)
					{
						case "Game":
							if (string.IsNullOrEmpty(tag.Value) || (tag.Value.Length <= 4) ||
							    !tag.Value.StartsWith("app_", StringComparison.Ordinal))
							{
								break;
							}

							string appIDText = tag.Value[4..];

							if (!uint.TryParse(appIDText, out uint appID) || (appID == 0))
							{
								break;
							}

							return appID;
					}
				}

				return 0;
			}
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
		private string ClassIDText
		{
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					return;
				}

				if (!ulong.TryParse(value, out ulong classID) || (classID == 0))
				{
					return;
				}

				ClassID = classID;
			}
		}

		[JsonProperty("instanceid", Required = Required.DisallowNull)]
		private string InstanceIDText
		{
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					return;
				}

				if (!ulong.TryParse(value, out ulong instanceID))
				{
					return;
				}

				InstanceID = instanceID;
			}
		}

		[JsonProperty("marketable", Required = Required.Always)]
		private byte MarketableNumber
		{
			set => Marketable = value > 0;
		}

		[JsonProperty("tradable", Required = Required.Always)]
		private byte TradableNumber
		{
			set => Tradable = value > 0;
		}

		[JsonConstructor]
		internal Description()
		{
		}
	}
}