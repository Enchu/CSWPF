using System;
using JetBrains.Annotations;

namespace CSWPF.Web;

public sealed class Web
{
    [PublicAPI]
    public const byte MaxTries = 5;
    private const ushort ExtendedTimeout = 600;
    private const byte MaxIdleTime = 15;
    
    [Flags]
    public enum ERequestOptions : byte {
        None = 0,
        ReturnClientErrors = 1,
        ReturnServerErrors = 2,
        ReturnRedirections = 4,
        AllowInvalidBodyOnSuccess = 8,
        AllowInvalidBodyOnErrors = 16
    }
}