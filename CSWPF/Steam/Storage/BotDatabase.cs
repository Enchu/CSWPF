using System;
using CSWPF.Steam.Security;
using CSWPF.Web.Core;
using CSWPF.Web.Assistance;
using Newtonsoft.Json;

namespace CSWPF.Steam.Storage;

public sealed class BotDatabase
{
    [JsonProperty($"_{nameof(MobileAuthenticator)}")]
    private MobileAuthenticator? BackingMobileAuthenticator;
    internal MobileAuthenticator? MobileAuthenticator {
        get => BackingMobileAuthenticator;

        set {
            if (BackingMobileAuthenticator == value) {
                return;
            }

            BackingMobileAuthenticator = value;
            //Utilities.InBackground(Save);
        }
    }
}