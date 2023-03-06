using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SteamKit2;

namespace CSWPF.Steam;

public class Bot
{
    [JsonIgnore]
    public static SteamConfiguration SteamConfiguration { get; }
}