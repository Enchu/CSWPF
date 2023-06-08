using System;
using Newtonsoft.Json;

namespace CSWPF.Steam.Data;

public sealed class Tag {
    [JsonProperty("category", Required = Required.Always)]
    public string Identifier { get; private set; } = "";

    [JsonProperty("internal_name", Required = Required.Always)]
    public string Value { get; private set; } = "";

    internal Tag(string identifier, string value) {
        Identifier = !string.IsNullOrEmpty(identifier) ? identifier : throw new ArgumentNullException(nameof(identifier));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    [System.Text.Json.Serialization.JsonConstructor]
    private Tag() { }
}