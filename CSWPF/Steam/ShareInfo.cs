using System;
using System.IO;
using System.Reflection;
using CSWPF.Web.Core;
using JetBrains.Annotations;

namespace CSWPF.Steam;

public static class SharedInfo {
	public const string ConfigDirectory = "config";

	internal const ulong ArchiSteamID = 76561198006963719;
	internal const string ArchivalLogFile = "log.{#}.txt";
	internal const string ArchivalLogsDirectory = "logs";
	internal const string ASF = nameof(ASF);
	internal const ulong ASFGroupSteamID = 103582791440160998;
	internal const string AssemblyDocumentation = $"{AssemblyName}.xml";
	internal const string AssemblyName = nameof(CSWPF);
	internal const string DatabaseExtension = ".db";
	internal const string DebugDirectory = "debug";
	internal const string EnvironmentVariableCryptKey = $"{ASF}_CRYPTKEY";
	internal const string EnvironmentVariableCryptKeyFile = $"{EnvironmentVariableCryptKey}_FILE";
	internal const string EnvironmentVariableNetworkGroup = $"{ASF}_NETWORK_GROUP";
	internal const string EnvironmentVariablePath = $"{ASF}_PATH";
	internal const string GithubRepo = $"JustArchiNET/{AssemblyName}";
	internal const string GlobalConfigFileName = $"{ASF}{JsonConfigExtension}";
	internal const string GlobalDatabaseFileName = $"{ASF}{DatabaseExtension}";
	internal const ushort InformationDelay = 10000;
	internal const string IPCConfigExtension = ".config";
	//internal const string IPCConfigFile = $"{nameof(IPC)}{IPCConfigExtension}";
	internal const string JsonConfigExtension = ".json";
	internal const string KeysExtension = ".keys";
	internal const string KeysUnusedExtension = ".unused";
	internal const string KeysUsedExtension = ".used";
	internal const string LicenseName = "Apache 2.0";
	internal const string LicenseURL = "https://www.apache.org/licenses/LICENSE-2.0";
	internal const string LogFile = "log.txt";
	internal const string LolcatCultureName = "qps-Ploc";
	internal const string MobileAuthenticatorExtension = ".maFile";
	internal const string PluginsDirectory = "plugins";
	internal const string SentryHashExtension = ".bin";
	internal const ushort ShortInformationDelay = InformationDelay / 2;
	internal const string UlongCompatibilityStringPrefix = "s_";
	internal const string UpdateDirectory = "_old";
	internal const string WebsiteDirectory = "www";

	internal static string HomeDirectory {
		get {
			if (!string.IsNullOrEmpty(CachedHomeDirectory)) {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				return CachedHomeDirectory!;
			}

			// We're aiming to handle two possible cases here, classic publish and single-file publish which is possible with OS-specific builds
			// In order to achieve that, we have to guess the case above from the binary's name
			// We can't just return our base directory since it could lead to the (wrong) temporary directory of extracted files in a single-publish scenario
			// If the path goes to our own binary, the user is using OS-specific build, single-file or not, we'll use path to location of that binary then
			// Otherwise, this path goes to some third-party binary, likely dotnet/mono, the user is using our generic build or other custom binary, we need to trust our base directory then
			//CachedHomeDirectory = Path.GetFileNameWithoutExtension(OS.ProcessFileName) == AssemblyName ? Path.GetDirectoryName(OS.ProcessFileName) ?? AppContext.BaseDirectory : AppContext.BaseDirectory;

			return CachedHomeDirectory;
		}
	}

	//internal static string ProgramIdentifier => $"{PublicIdentifier} V{Version} ({BuildInfo.Variant}/{ModuleVersion} | {OS.Version})";
	//internal static string PublicIdentifier => $"{AssemblyName}{(BuildInfo.IsCustomBuild ? "-custom" : PluginsCore.HasCustomPluginsLoaded ? "-modded" : "")}";
	internal static Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException(nameof(Version));

	private static Guid ModuleVersion => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId;

	private static string? CachedHomeDirectory;

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
}