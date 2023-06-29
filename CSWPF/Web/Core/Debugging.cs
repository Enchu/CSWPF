using System;
using CSWPF.Utils;
using SteamKit2;

namespace CSWPF.Web.Core;

internal static class Debugging {
    internal static bool IsDebugBuild => true;
    
    internal static bool IsDebugConfigured => GlobalConfig.DefaultDebug;
    internal static bool IsUserDebugging => IsDebugBuild || IsDebugConfigured;
}