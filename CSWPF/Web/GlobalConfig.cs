using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SteamKit2;

namespace CSWPF.Web;

public class GlobalConfig
{
    public const byte DefaultConnectionTimeout = 90;
    public const byte DefaultInventoryLimiterDelay = 4;
    public const ushort DefaultWebLimiterDelay = 300;
    [JsonProperty(Required = Required.DisallowNull)]
    [Range(1, byte.MaxValue)]
    public byte ConnectionTimeout { get; private set; } = DefaultConnectionTimeout;
    [JsonProperty(Required = Required.DisallowNull)]
    [Range(ushort.MinValue, ushort.MaxValue)]
    public ushort WebLimiterDelay { get; private set; } = DefaultWebLimiterDelay;
    [JsonProperty(Required = Required.DisallowNull)]
    [Range(byte.MinValue, byte.MaxValue)]
    public byte InventoryLimiterDelay { get; private set; } = DefaultInventoryLimiterDelay;
    public const ProtocolTypes DefaultSteamProtocols = ProtocolTypes.All;
    public ProtocolTypes SteamProtocols { get; private set; } = DefaultSteamProtocols;
}