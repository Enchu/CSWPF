using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace CSWPF.Web;

public class WebBrowser
{
	public const byte MaxTries = 5;

    internal const byte MaxConnections = 5;

    private const ushort ExtendedTimeout = 600;
    private const byte MaxIdleTime = 15;
    public static TimeSpan Timeout => HttpClient.Timeout;
    
    private static readonly HttpClient HttpClient;
    private static readonly HttpClientHandler HttpClientHandler;
    internal static class BuildInfo {
#if ASF_VARIANT_DOCKER
		internal static bool CanUpdate => false;
		internal static string Variant => "docker";
#elif ASF_VARIANT_GENERIC
		internal static bool CanUpdate => true;
		internal static string Variant => "generic";
#elif ASF_VARIANT_GENERIC_NETF
		internal static bool CanUpdate => true;
		internal static string Variant => "generic-netf";
#elif ASF_VARIANT_LINUX_ARM
		internal static bool CanUpdate => true;
		internal static string Variant => "linux-arm";
#elif ASF_VARIANT_LINUX_ARM64
		internal static bool CanUpdate => true;
		internal static string Variant => "linux-arm64";
#elif ASF_VARIANT_LINUX_X64
		internal static bool CanUpdate => true;
		internal static string Variant => "linux-x64";
#elif ASF_VARIANT_OSX_ARM64
		internal static bool CanUpdate => true;
		internal static string Variant => "osx-arm64";
#elif ASF_VARIANT_OSX_X64
		internal static bool CanUpdate => true;
		internal static string Variant => "osx-x64";
#elif ASF_VARIANT_WIN_X64
		internal static bool CanUpdate => true;
		internal static string Variant => "win-x64";
#else
        internal static bool CanUpdate => false;
        internal static string Variant => SourceVariant;
#endif

        private const string SourceVariant = "source";

        internal static bool IsCustomBuild => Variant == SourceVariant;
    }
    
    internal static Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));
    internal static ImmutableHashSet<IPlugin>? ActivePlugins { get; private set; }
    internal static bool HasCustomPluginsLoaded => ActivePlugins?.Any(static plugin => plugin is not OfficialPlugin officialPlugin || !officialPlugin.HasSameVersion()) == true;
    internal static string PublicIdentifier => $"{typeof(AssemblyName)}{(BuildInfo.IsCustomBuild ? "-custom" : HasCustomPluginsLoaded ? "-modded" : "")}";
    
    public static HttpClient GenerateDisposableHttpClient(bool extendedTimeout = false) {
        byte connectionTimeout = ASF.GlobalConfig?.ConnectionTimeout ?? GlobalConfig.DefaultConnectionTimeout;

        HttpClient result = new(HttpClientHandler, false) {
#if !NETFRAMEWORK
            DefaultRequestVersion = HttpVersion.Version30,
#endif
            Timeout = TimeSpan.FromSeconds(extendedTimeout ? ExtendedTimeout : connectionTimeout)
        };
        
        result.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(PublicIdentifier, Version.ToString()));
        //result.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({BuildInfo.Variant}; {OS.Version.Replace("(", "", StringComparison.Ordinal).Replace(")", "", StringComparison.Ordinal)}; +{SharedInfo.ProjectURL})"));

        result.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.9));
        result.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.8));

        return result;
    }
}

public interface IPlugin {

	string Name { get; }
	Version Version { get; }
	Task OnLoaded();
}

internal abstract class OfficialPlugin : IPlugin {
	public abstract string Name { get; }
	public abstract Version Version { get; }
	public abstract Task OnLoaded();
	internal bool HasSameVersion() => Version == WebBrowser.Version;
}