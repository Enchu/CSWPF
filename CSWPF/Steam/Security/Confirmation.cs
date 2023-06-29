using System;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Steam.Security;

public sealed class Confirmation {
    [JsonProperty(Required = Required.Always)]
    public ulong Creator { get; }

    [JsonProperty(Required = Required.Always)]
    public ulong ID { get; }

    [JsonProperty(Required = Required.Always)]
    public ulong Key { get; }

    [JsonProperty(Required = Required.Always)]
    public EType Type { get; }

    internal Confirmation(ulong id, ulong key, ulong creator, EType type) {
        ID = id > 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
        Key = key > 0 ? key : throw new ArgumentOutOfRangeException(nameof(key));
        Creator = creator > 0 ? creator : throw new ArgumentOutOfRangeException(nameof(creator));
        Type = Enum.IsDefined(type) ? type : throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(EType));
    }
    
    public enum EType : byte {
        Unknown,
        Generic,
        Trade,
        Market,

        // We're missing information about definition of number 4 type
        PhoneNumberChange = 5,
        AccountRecovery = 6
    }
}