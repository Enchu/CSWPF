using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CSWPF.Directory;

public class Inventory
{
    [JsonProperty("assets")]
    public Asset[] Assets { get; set; }
    [JsonProperty("descriptions")]
    public Description[] Descriptions { get; set; }
    [JsonProperty("total_inventory_count")]
    public long TotalInventoryCount { get; set; }
}
public class Asset
{
    [JsonProperty("contextid")] 
    public long Contextid { get; set; }

    [JsonProperty("assetid")]
    public string Assetid { get; set; }

    [JsonProperty("classid")]
    public string Classid { get; set; }
}

public class Description
{
    [JsonProperty("appid")]
    public long Appid { get; set; }

    [JsonProperty("classid")]
    public string Classid { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
}