using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Directory;

public class Tag
{
    [JsonProperty("category", Required = Required.Always)]
    [PublicAPI]
    public string Identifier { get; private set; } = "";

    [JsonProperty("internal_name", Required = Required.Always)]
    [PublicAPI]
    public string Value { get; private set; } = "";

    internal Tag(string identifier, string value) {
        Identifier = !string.IsNullOrEmpty(identifier) ? identifier : throw new ArgumentNullException(nameof(identifier));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    [JsonConstructor]
    private Tag() { }
}