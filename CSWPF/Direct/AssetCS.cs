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
    
    [JsonProperty("appid", Required = Required.DisallowNull)]
	public long AppID { get; private set; }
	
	[System.Text.Json.Serialization.JsonIgnore]
	public long ContextID { get; private set; }
	
	[System.Text.Json.Serialization.JsonIgnore]
	public long Amount { get; internal set; }

	[System.Text.Json.Serialization.JsonIgnore]
	public ulong AssetID { get; private set; }
}