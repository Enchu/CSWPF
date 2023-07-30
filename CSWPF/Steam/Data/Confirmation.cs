using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Steam.Security;

public sealed class Confirmation {
    [JsonProperty(PropertyName = "nonce", Required = Required.Always)]
    internal readonly ulong Nonce;

    [JsonProperty(PropertyName = "type", Required = Required.Always)]
    public EConfirmationType ConfirmationType { get; private set; }

    [JsonProperty(PropertyName = "creator_id", Required = Required.Always)]
    public ulong CreatorID { get; private set; }

    [JsonProperty(PropertyName = "id", Required = Required.Always)]
    public ulong ID { get; private set; }

    [JsonConstructor]
    private Confirmation() { }

    [PublicAPI]
    public enum EConfirmationType : byte {
        Unknown,
        Generic,
        Trade,
        Market,

        // We're missing information about definition of number 4 type
        PhoneNumberChange = 5,
        AccountRecovery = 6
    }
}