using System;
using System.Collections.Immutable;
using System.Threading;
using CSWPF.Steam;
using JetBrains.Annotations;

namespace CSWPF.Web;

public class ASF
{
    public static GlobalConfig? GlobalConfig { get; internal set; }
    internal static readonly SemaphoreSlim OpenConnectionsSemaphore = new(WebBrowser.MaxConnections, WebBrowser.MaxConnections);
    internal static ICrossProcessSemaphore? RateLimitingSemaphore { get; private set; }
    internal static ImmutableDictionary<Uri, (ICrossProcessSemaphore RateLimitingSemaphore, SemaphoreSlim OpenConnectionsSemaphore)>? WebLimitingSemaphores { get; private set; }
}