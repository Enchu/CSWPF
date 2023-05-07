using System.Windows.Documents;
using Newtonsoft.Json;

namespace CSWPF.Directory;

public class maFile
{
    public string FileName { get; set; }
    [JsonProperty("shared_secret")]
    public string SharedSecret { get; set; }
    [JsonProperty("account_name")]
    public string AccountName { get; set; }
    [JsonProperty("Session")]
    public Session Session { get; set; }
}
public class Session
{
    [JsonProperty("SteamID")]
    public ulong SteamId { get; set; }
}

