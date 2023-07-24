using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using CSWPF.Steam.Data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace CSWPF.Directory;

public class InventoryResponseCS: OptionalResultResponse {
    
        [JsonProperty("assets")]
        public Asset[] Assets { get; set; }

        [JsonProperty("descriptions")]
        public Description[] Descriptions { get; set; }

        [JsonProperty("total_inventory_count")]
        public long TotalInventoryCount { get; set; }

        [JsonProperty("rwgrsn")]
        public long Rwgrsn { get; set; }

    public class Asset
    {
        [JsonProperty("appid")]
        public uint  Appid { get; set; }
        [JsonProperty("contextid")]
        public string  Contextid { get; set; }
        [JsonProperty("amount")]
        public uint  Amount { get; set; }
        [JsonProperty("assetid")]
        public string  Assetid { get; set; }
        [JsonProperty("classid")]
        public ulong Classid { get; set; }
        [JsonProperty("instanceid")]
        public ulong Instanceid { get; set; }
        [JsonProperty("tradable")]
        public bool  Tradable { get; set; }
        [JsonProperty("marketable")]
        public bool Marketable { get; set; }
        [JsonProperty("tags")]
        public Tag[] Tags { get; set; }
    }

    public class Description
    {
        [JsonProperty("appid")]
        public long Appid { get; set; }

        [JsonProperty("classid")]
        public string Classid { get; set; }

        [JsonProperty("instanceid")]
        public long Instanceid { get; set; }

        [JsonProperty("currency")]
        public long Currency { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }
        [JsonProperty("descriptions")]
        public DescriptionDescription[] Descriptions { get; set; }
        
        [JsonProperty("tradable")]
        public long Tradable { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColor { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("commodity")]
        public long Commodity { get; set; }

        [JsonProperty("market_tradable_restriction")]
        public long MarketTradableRestriction { get; set; }

        [JsonProperty("marketable")]
        public long Marketable { get; set; }

        [JsonProperty("tags")]
        public Tag[] Tags { get; set; }

        [JsonProperty("market_buy_country_restriction")]
        public string MarketBuyCountryRestriction { get; set; }
    }
    
    public class AssetCS
    {
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
    
    public enum TypeEnum { Html };
    public partial class DescriptionDescription
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string Color { get; set; }
    }

    public class Tag
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("internal_name")]
        public string InternalName { get; set; }

        [JsonProperty("localized_category_name")]
        public string LocalizedCategoryName { get; set; }

        [JsonProperty("localized_tag_name")]
        public string LocalizedTagName { get; set; }

        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        public string Color { get; set; }
    }
    
    [JsonConstructor]
    public InventoryResponseCS()
    {
        
    }
}