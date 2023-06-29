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
    [JsonProperty("appid")]
    public uint AppID { get; set; }
    [JsonProperty("contextid")]
    public string ContextID { get; set; }
    [JsonProperty("amount")]
    public uint Amount { get; set; }
    [JsonProperty("assetid")]
    public string AssetID { get; set; }
    [JsonProperty("classid")]
    public ulong ClassID { get; set; }
    [JsonProperty("instanceid")]
    public ulong InstanceID { get; set; }
    [JsonIgnore]
    public bool Tradable { get; set; }
    [JsonIgnore]
    public string MarketName = null;
    [JsonIgnore]
    public bool Marketable;
    [JsonConstructor]
    public AssetCS()
    {

    }
}