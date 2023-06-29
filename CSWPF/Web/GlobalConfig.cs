using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CSWPF.Utils;
using CSWPF.Web.Core;
using CSWPF.Web.Assistance;
using JetBrains.Annotations;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace CSWPF.Web;

public sealed class GlobalConfig {
	public const bool DefaultAutoRestart = true;
	public const string? DefaultCommandPrefix = "!";
	public const byte DefaultConfirmationsLimiterDelay = 10;
	public const byte DefaultConnectionTimeout = 90;
	public const string? DefaultCurrentCulture = null;
	public const bool DefaultDebug = false;
	public const byte DefaultFarmingDelay = 15;
	public const bool DefaultFilterBadBots = true;
	public const byte DefaultGiftsLimiterDelay = 1;
	public const bool DefaultHeadless = false;
	public const byte DefaultIdleFarmingPeriod = 8;
	public const byte DefaultInventoryLimiterDelay = 4;
	public const bool DefaultIPC = true;
	public const string? DefaultIPCPassword = null;
	public const byte DefaultLoginLimiterDelay = 10;
	public const byte DefaultMaxFarmingTime = 10;
	public const byte DefaultMaxTradeHoldDuration = 15;
	public const byte DefaultMinFarmingDelayAfterBlock = 60;
	public const EOptimizationMode DefaultOptimizationMode = EOptimizationMode.MaxPerformance;
	public const string? DefaultSteamMessagePrefix = "/me ";
	public const ulong DefaultSteamOwnerID = 0;
	public const ProtocolTypes DefaultSteamProtocols = ProtocolTypes.All;
	public const EUpdateChannel DefaultUpdateChannel = EUpdateChannel.Stable;
	public const byte DefaultUpdatePeriod = 24;
	public const ushort DefaultWebLimiterDelay = 300;
	public const string? DefaultWebProxyPassword = null;
	public const string? DefaultWebProxyText = null;
	public const string? DefaultWebProxyUsername = null;
	public static readonly ImmutableHashSet<uint> DefaultBlacklist = ImmutableHashSet<uint>.Empty;
	public static readonly Guid? DefaultLicenseID;
	[PublicAPI]
	public WebProxy? WebProxy {
		get {
			if (BackingWebProxy != null) {
				return BackingWebProxy;
			}

			if (string.IsNullOrEmpty(WebProxyText)) {
				return null;
			}

			Uri uri;

			try {
				uri = new Uri(WebProxyText!);
			} catch (UriFormatException e) {
				return null;
			}

			WebProxy proxy = new() {
				Address = uri,
				BypassProxyOnLocal = true
			};

			if (!string.IsNullOrEmpty(WebProxyUsername) || !string.IsNullOrEmpty(WebProxyPassword)) {
				NetworkCredential credentials = new();

				if (!string.IsNullOrEmpty(WebProxyUsername)) {
					credentials.UserName = WebProxyUsername;
				}

				if (!string.IsNullOrEmpty(WebProxyPassword)) {
					credentials.Password = WebProxyPassword;
				}

				proxy.Credentials = credentials;
			}

			BackingWebProxy = proxy;

			return proxy;
		}
	}

	[JsonProperty(Required = Required.DisallowNull)]
	public bool AutoRestart { get; private set; } = DefaultAutoRestart;

	[JsonProperty(Required = Required.DisallowNull)]
	public ImmutableHashSet<uint> Blacklist { get; private set; } = DefaultBlacklist;

	[JsonProperty]
	public string? CommandPrefix { get; private set; } = DefaultCommandPrefix;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte ConfirmationsLimiterDelay { get; private set; } = DefaultConfirmationsLimiterDelay;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(1, byte.MaxValue)]
	public byte ConnectionTimeout { get; private set; } = DefaultConnectionTimeout;

	[JsonProperty]
	public string? CurrentCulture { get; private set; } = DefaultCurrentCulture;

	[JsonProperty(Required = Required.DisallowNull)]
	public bool Debug { get; private set; } = DefaultDebug;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(1, byte.MaxValue)]
	public byte FarmingDelay { get; private set; } = DefaultFarmingDelay;

	[JsonProperty(Required = Required.DisallowNull)]
	public bool FilterBadBots { get; private set; } = DefaultFilterBadBots;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte GiftsLimiterDelay { get; private set; } = DefaultGiftsLimiterDelay;

	[JsonProperty(Required = Required.DisallowNull)]
	public bool Headless { get; private set; } = DefaultHeadless;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte IdleFarmingPeriod { get; private set; } = DefaultIdleFarmingPeriod;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte InventoryLimiterDelay { get; private set; } = DefaultInventoryLimiterDelay;

	[JsonProperty(Required = Required.DisallowNull)]
	public bool IPC { get; private set; } = DefaultIPC;

	[JsonProperty]
	public string? IPCPassword {
		get => BackingIPCPassword;
		internal set {
			IsIPCPasswordSet = true;
			BackingIPCPassword = value;
		}
	}
	public Guid? LicenseID { get; private set; } = DefaultLicenseID;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte LoginLimiterDelay { get; private set; } = DefaultLoginLimiterDelay;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(1, byte.MaxValue)]
	public byte MaxFarmingTime { get; private set; } = DefaultMaxFarmingTime;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte MaxTradeHoldDuration { get; private set; } = DefaultMaxTradeHoldDuration;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte MinFarmingDelayAfterBlock { get; private set; } = DefaultMinFarmingDelayAfterBlock;

	[JsonProperty(Required = Required.DisallowNull)]
	public EOptimizationMode OptimizationMode { get; private set; } = DefaultOptimizationMode;
	public string? SteamMessagePrefix { get; private set; } = DefaultSteamMessagePrefix;
	public ulong SteamOwnerID { get; private set; } = DefaultSteamOwnerID;
	public ProtocolTypes SteamProtocols { get; private set; } = DefaultSteamProtocols;
	public EUpdateChannel UpdateChannel { get; private set; } = DefaultUpdateChannel;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(byte.MinValue, byte.MaxValue)]
	public byte UpdatePeriod { get; private set; } = DefaultUpdatePeriod;

	[JsonProperty(Required = Required.DisallowNull)]
	[Range(ushort.MinValue, ushort.MaxValue)]
	public ushort WebLimiterDelay { get; private set; } = DefaultWebLimiterDelay;
	public string? WebProxyText { get; private set; } = DefaultWebProxyText;
	public string? WebProxyUsername { get; private set; } = DefaultWebProxyUsername;
	
	internal Dictionary<string, JToken>? AdditionalProperties {
		get;
		[UsedImplicitly]
		set;
	}

	internal bool IsIPCPasswordSet { get; private set; }
	internal bool IsWebProxyPasswordSet { get; private set; }

	internal bool Saving { get; set; }

	internal string? WebProxyPassword {
		get => BackingWebProxyPassword;

		set {
			IsWebProxyPasswordSet = true;
			BackingWebProxyPassword = value;
		}
	}

	private string? BackingIPCPassword = DefaultIPCPassword;
	private WebProxy? BackingWebProxy;
	private string? BackingWebProxyPassword = DefaultWebProxyPassword;
	
	private string SSteamOwnerID {
		get => SteamOwnerID.ToString(CultureInfo.InvariantCulture);

		set {
			if (string.IsNullOrEmpty(value) || !ulong.TryParse(value, out ulong result)) {
				return;
			}

			SteamOwnerID = result;
		}
	}

	[JsonConstructor]
	internal GlobalConfig() { }
	public bool ShouldSerializeAutoRestart() => !Saving || (AutoRestart != DefaultAutoRestart);
	public bool ShouldSerializeBlacklist() => !Saving || ((Blacklist != DefaultBlacklist) && !Blacklist.SetEquals(DefaultBlacklist));
	public bool ShouldSerializeCommandPrefix() => !Saving || (CommandPrefix != DefaultCommandPrefix);
	public bool ShouldSerializeConfirmationsLimiterDelay() => !Saving || (ConfirmationsLimiterDelay != DefaultConfirmationsLimiterDelay);
	public bool ShouldSerializeConnectionTimeout() => !Saving || (ConnectionTimeout != DefaultConnectionTimeout);
	public bool ShouldSerializeCurrentCulture() => !Saving || (CurrentCulture != DefaultCurrentCulture);
	public bool ShouldSerializeDebug() => !Saving || (Debug != DefaultDebug);
	public bool ShouldSerializeFarmingDelay() => !Saving || (FarmingDelay != DefaultFarmingDelay);
	public bool ShouldSerializeFilterBadBots() => !Saving || (FilterBadBots != DefaultFilterBadBots);
	public bool ShouldSerializeGiftsLimiterDelay() => !Saving || (GiftsLimiterDelay != DefaultGiftsLimiterDelay);
	public bool ShouldSerializeHeadless() => !Saving || (Headless != DefaultHeadless);
	public bool ShouldSerializeIdleFarmingPeriod() => !Saving || (IdleFarmingPeriod != DefaultIdleFarmingPeriod);
	public bool ShouldSerializeInventoryLimiterDelay() => !Saving || (InventoryLimiterDelay != DefaultInventoryLimiterDelay);
	public bool ShouldSerializeIPC() => !Saving || (IPC != DefaultIPC);
	public bool ShouldSerializeIPCPassword() => Saving && IsIPCPasswordSet && (IPCPassword != DefaultIPCPassword);
	public bool ShouldSerializeLicenseID() => !Saving || ((LicenseID != DefaultLicenseID) && (LicenseID != Guid.Empty));
	public bool ShouldSerializeLoginLimiterDelay() => !Saving || (LoginLimiterDelay != DefaultLoginLimiterDelay);
	public bool ShouldSerializeMaxFarmingTime() => !Saving || (MaxFarmingTime != DefaultMaxFarmingTime);
	public bool ShouldSerializeMaxTradeHoldDuration() => !Saving || (MaxTradeHoldDuration != DefaultMaxTradeHoldDuration);
	public bool ShouldSerializeMinFarmingDelayAfterBlock() => !Saving || (MinFarmingDelayAfterBlock != DefaultMinFarmingDelayAfterBlock);
	public bool ShouldSerializeOptimizationMode() => !Saving || (OptimizationMode != DefaultOptimizationMode);
	public bool ShouldSerializeSSteamOwnerID() => !Saving;
	public bool ShouldSerializeSteamMessagePrefix() => !Saving || (SteamMessagePrefix != DefaultSteamMessagePrefix);
	public bool ShouldSerializeSteamOwnerID() => !Saving || (SteamOwnerID != DefaultSteamOwnerID);
	public bool ShouldSerializeSteamProtocols() => !Saving || (SteamProtocols != DefaultSteamProtocols);
	public bool ShouldSerializeUpdateChannel() => !Saving || (UpdateChannel != DefaultUpdateChannel);
	public bool ShouldSerializeUpdatePeriod() => !Saving || (UpdatePeriod != DefaultUpdatePeriod);
	public bool ShouldSerializeWebLimiterDelay() => !Saving || (WebLimiterDelay != DefaultWebLimiterDelay);
	public bool ShouldSerializeWebProxyPassword() => Saving && IsWebProxyPasswordSet && (WebProxyPassword != DefaultWebProxyPassword);
	public bool ShouldSerializeWebProxyText() => !Saving || (WebProxyText != DefaultWebProxyText);
	public bool ShouldSerializeWebProxyUsername() => !Saving || (WebProxyUsername != DefaultWebProxyUsername);
	internal (bool Valid, string? ErrorMessage) CheckValidation() {
		if (Blacklist.Contains(0)) {
			return (false, string.Format(CultureInfo.CurrentCulture, nameof(Blacklist), 0));
		}

		if (ConnectionTimeout == 0) {
			return (false, string.Format(CultureInfo.CurrentCulture, nameof(ConnectionTimeout), ConnectionTimeout));
		}

		if (FarmingDelay == 0) {
			return (false, string.Format(CultureInfo.CurrentCulture,nameof(FarmingDelay), FarmingDelay));
		}
		

		if (MaxFarmingTime == 0) {
			return (false, string.Format(CultureInfo.CurrentCulture, nameof(MaxFarmingTime), MaxFarmingTime));
		}

		if (!Enum.IsDefined(OptimizationMode)) {
			return (false, string.Format(CultureInfo.CurrentCulture,nameof(OptimizationMode), OptimizationMode));
		}

		if ((SteamOwnerID != 0) && !new SteamID(SteamOwnerID).IsIndividualAccount) {
			return (false, string.Format(CultureInfo.CurrentCulture, nameof(SteamOwnerID), SteamOwnerID));
		}

		if (SteamProtocols is <= 0 or > ProtocolTypes.All) {
			return (false, string.Format(CultureInfo.CurrentCulture, nameof(SteamProtocols), SteamProtocols));
		}

		return Enum.IsDefined(UpdateChannel) ? (true, null) : (false, string.Format(CultureInfo.CurrentCulture, nameof(UpdateChannel), UpdateChannel));
	}

	internal static async Task<(GlobalConfig? GlobalConfig, string? LatestJson)> Load(string filePath) {
		if (string.IsNullOrEmpty(filePath)) {
			throw new ArgumentNullException(nameof(filePath));
		}

		if (!File.Exists(filePath)) {
			return (null, null);
		}

		string json;
		GlobalConfig? globalConfig;

		try {
			json = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

			if (string.IsNullOrEmpty(json)) {
				return (null, null);
			}

			globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(json);
		} catch (Exception e) {
			Msg.ShowError(e.ToString());
			return (null, null);
		}

		if (globalConfig == null) {
			return (null, null);
		}

		(bool valid, string? errorMessage) = globalConfig.CheckValidation();

		if (!valid) {
			return (null, null);
		}

		globalConfig.Saving = true;
		string latestJson = JsonConvert.SerializeObject(globalConfig, Formatting.Indented);
		globalConfig.Saving = false;

		return (globalConfig, json != latestJson ? latestJson : null);
	}

	internal static async Task<bool> Write(string filePath, GlobalConfig globalConfig) {
		if (string.IsNullOrEmpty(filePath)) {
			throw new ArgumentNullException(nameof(filePath));
		}

		ArgumentNullException.ThrowIfNull(globalConfig);

		string json = JsonConvert.SerializeObject(globalConfig, Formatting.Indented);

		return await SerializableFile.Write(filePath, json).ConfigureAwait(false);
	}
	public enum EOptimizationMode : byte {
		MaxPerformance,
		MinMemoryUsage
	}
	public enum EUpdateChannel : byte {
		None,
		Stable,
		Experimental
	}
}