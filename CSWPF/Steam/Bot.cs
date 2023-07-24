using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Steam.Integration;
using CSWPF.Steam.Interaction;
using CSWPF.Steam.Plugns;
using CSWPF.Steam.Security;
using CSWPF.Steam.Storage;
using CSWPF.Utils;
using CSWPF.Web;
using CSWPF.Web.Core;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SteamKit2;
using SteamKit2.Discovery;
using SteamKit2.Internal;
using Utilities = CSWPF.Web.Core.Utilities;

namespace CSWPF.Steam;

public sealed class Bot : IAsyncDisposable, IDisposable {
	private const char DefaultBackgroundKeysRedeemerSeparator = '\t';
	internal const ushort CallbackSleep = 500; // In milliseconds
	private readonly static SemaphoreSlim LoginSemaphore = new(1, 1);
	private readonly static SemaphoreSlim LoginRateLimitingSemaphore = new(1, 1);
	private readonly static SemaphoreSlim RateLimitingSemaphore = new(1, 1);
	public User user;
	public static IReadOnlyDictionary<string, Bot>? BotsReadOnly => Bots;

	internal static ConcurrentDictionary<string, Bot>? Bots { get; private set; }
	internal static StringComparer? BotsComparer { get; private set; }
	internal static EOSType OSType { get; private set; } = EOSType.Unknown;

	private static readonly SemaphoreSlim BotsSemaphore = new(1, 1);
	public WebHandler WebHandler { get; set; }
	public Actions Actions { get; }

	[JsonProperty]
	public string BotName { get; }
	[JsonProperty]
	public static bool IsConnectedAndLoggedOn => SteamClient.SteamID != null;

	[JsonIgnore]
	public SteamConfiguration SteamConfiguration { get; }
	internal static readonly BotDatabase BotDatabase;
	public bool HasMobileAuthenticator => BotDatabase.MobileAuthenticator != null;
	[JsonIgnore]
	public SteamFriends SteamFriends { get; }
	
	internal bool IsAccountLimited => AccountFlags.HasFlag(EAccountFlags.LimitedUser) || AccountFlags.HasFlag(EAccountFlags.LimitedUserForce);
	internal bool IsAccountLocked => AccountFlags.HasFlag(EAccountFlags.Lockdown);

	private readonly CallbackManager CallbackManager;
	private readonly SemaphoreSlim CallbackSemaphore = new(1, 1);
	private readonly SemaphoreSlim GamesRedeemerInBackgroundSemaphore = new(1, 1);
	private readonly Timer HeartBeatTimer;
	private readonly SemaphoreSlim InitializationSemaphore = new(1, 1);
	private readonly SemaphoreSlim MessagingSemaphore = new(1, 1);
	private readonly SemaphoreSlim SendCompleteTypesSemaphore = new(1, 1);
	private static SteamClient SteamClient;
	private readonly SteamUser SteamUser;

	private IEnumerable<(string FilePath, EFileType FileType)> RelatedFiles {
		get {
			foreach (EFileType fileType in Enum.GetValues(typeof(EFileType))) {
				string filePath = GetFilePath(fileType);

				if (string.IsNullOrEmpty(filePath)) {

					yield break;
				}

				yield return (filePath, fileType);
			}
		}
	}

	[JsonProperty($"{SharedInfo.UlongCompatibilityStringPrefix}{nameof(SteamID)}")]
	private string SSteamID => SteamID.ToString(CultureInfo.InvariantCulture);

	[JsonProperty]
	public EAccountFlags AccountFlags { get; private set; }

	[JsonProperty]
	public bool KeepRunning { get; private set; }

	[JsonProperty]
	public string? Nickname { get; private set; }

	[JsonIgnore]
	public ImmutableDictionary<uint, (EPaymentMethod PaymentMethod, DateTime TimeCreated)> OwnedPackageIDs { get; private set; } = ImmutableDictionary<uint, (EPaymentMethod PaymentMethod, DateTime TimeCreated)>.Empty;

	[JsonProperty]
	public static ulong SteamID { get; private set; }

	[JsonProperty]
	public long WalletBalance { get; private set; }

	[JsonProperty]
	public ECurrencyCode WalletCurrency { get; private set; }

	internal byte HeartBeatFailures { get; private set; }
	internal bool PlayingBlocked { get; private set; }
	internal bool PlayingWasBlocked { get; private set; }

	private string? AuthCode;

	[JsonProperty]
	private string? AvatarHash;

	private Timer? ConnectionFailureTimer;
	private bool FirstTradeSent;
	private Timer? GamesRedeemerInBackgroundTimer;
	private EResult LastLogOnResult;
	private DateTime LastLogonSessionReplaced;
	private bool LibraryLocked;
	private byte LoginFailures;
	private ulong MasterChatGroupID;
	private Timer? PlayingWasBlockedTimer;
	private bool ReconnectOnUserInitiated;
	private bool SendCompleteTypesScheduled;
	private Timer? SendItemsTimer;
	private bool SteamParentalActive;
	private string? TwoFactorCode;
	public ArchiHandler ArchiHandler { get; }

	public Bot(User userConfig)
	{
		user = userConfig;
		
		//BotDatabase.MobileAuthenticator?.Init(this);
		
		WebHandler = new WebHandler(this);

		SteamConfiguration = SteamConfiguration.Create(
			builder => {
				builder.WithCellID(0);
				builder.WithHttpClientFactory(WebHandler.GenerateDisposableHttpClient);
				builder.WithProtocolTypes(ProtocolTypes.All);
				builder.WithServerListProvider(new MemoryServerListProvider());
			}
		);

		// Initialize
		SteamClient = new SteamClient(SteamConfiguration, userConfig.Login);

		SteamUnifiedMessages? steamUnifiedMessages = SteamClient.GetHandler<SteamUnifiedMessages>();

		ArchiHandler = new ArchiHandler(steamUnifiedMessages ?? throw new InvalidOperationException(nameof(steamUnifiedMessages)));
		SteamClient.AddHandler(ArchiHandler);

		CallbackManager = new CallbackManager(SteamClient);
		CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
		CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
		
		SteamUser = SteamClient.GetHandler<SteamUser>() ?? throw new InvalidOperationException(nameof(SteamUser));
		CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
		CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
		//CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);
		//CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
		//CallbackManager.Subscribe<SteamUser.WalletInfoCallback>(OnWalletUpdate);

	}

	public void Dispose() {
		// Those are objects that are always being created if constructor doesn't throw exception
		WebHandler.Dispose();
		CallbackSemaphore.Dispose();
		GamesRedeemerInBackgroundSemaphore.Dispose();
		InitializationSemaphore.Dispose();
		MessagingSemaphore.Dispose();
		SendCompleteTypesSemaphore.Dispose();
		HeartBeatTimer.Dispose();

		// Those are objects that might be null and the check should be in-place
		ConnectionFailureTimer?.Dispose();
		GamesRedeemerInBackgroundTimer?.Dispose();
		PlayingWasBlockedTimer?.Dispose();
		SendItemsTimer?.Dispose();
	}

	public async ValueTask DisposeAsync() {
		// Those are objects that are always being created if constructor doesn't throw exception
		WebHandler.Dispose();
		CallbackSemaphore.Dispose();
		GamesRedeemerInBackgroundSemaphore.Dispose();
		InitializationSemaphore.Dispose();
		MessagingSemaphore.Dispose();
		SendCompleteTypesSemaphore.Dispose();
		
		await HeartBeatTimer.DisposeAsync().ConfigureAwait(false);

		// Those are objects that might be null and the check should be in-place
		if (ConnectionFailureTimer != null) {
			await ConnectionFailureTimer.DisposeAsync().ConfigureAwait(false);
		}

		if (GamesRedeemerInBackgroundTimer != null) {
			await GamesRedeemerInBackgroundTimer.DisposeAsync().ConfigureAwait(false);
		}

		if (PlayingWasBlockedTimer != null) {
			await PlayingWasBlockedTimer.DisposeAsync().ConfigureAwait(false);
		}

		if (SendItemsTimer != null) {
			await SendItemsTimer.DisposeAsync().ConfigureAwait(false);
		}
	}

	public static string GetFilePath(string botName, EFileType fileType) {
		if (string.IsNullOrEmpty(botName)) {
			throw new ArgumentNullException(nameof(botName));
		}

		if (!Enum.IsDefined(fileType)) {
			throw new InvalidEnumArgumentException(nameof(fileType), (int) fileType, typeof(EFileType));
		}

		string botPath = Path.Combine(SharedInfo.ConfigDirectory, botName);

		return fileType switch {
			EFileType.Config => $"{botPath}{SharedInfo.JsonConfigExtension}",
			EFileType.Database => $"{botPath}{SharedInfo.DatabaseExtension}",
			EFileType.KeysToRedeem => $"{botPath}{SharedInfo.KeysExtension}",
			EFileType.KeysToRedeemUnused => $"{botPath}{SharedInfo.KeysExtension}{SharedInfo.KeysUnusedExtension}",
			EFileType.KeysToRedeemUsed => $"{botPath}{SharedInfo.KeysExtension}{SharedInfo.KeysUsedExtension}",
			EFileType.MobileAuthenticator => $"{botPath}{SharedInfo.MobileAuthenticatorExtension}",
			EFileType.SentryFile => $"{botPath}{SharedInfo.SentryHashExtension}",
			_ => throw new ArgumentOutOfRangeException(nameof(fileType))
		};
	}

	public string GetFilePath(EFileType fileType) {
		if (!Enum.IsDefined(fileType)) {
			throw new InvalidEnumArgumentException(nameof(fileType), (int) fileType, typeof(EFileType));
		}

		return GetFilePath(BotName, fileType);
	}

	internal static void Init(StringComparer botsComparer, IMachineInfoProvider? customMachineInfoProvider = null) {
		if (Bots != null) {
			throw new InvalidOperationException(nameof(Bots));
		}

		BotsComparer = botsComparer ?? throw new ArgumentNullException(nameof(botsComparer));
		Bots = new ConcurrentDictionary<string, Bot>(botsComparer);
	}

	internal async Task<bool> RefreshSession() {
		if (!IsConnectedAndLoggedOn) {
			return false;
		}

		SteamUser.WebAPIUserNonceCallback callback;

		try {
			callback = await SteamUser.RequestWebAPIUserNonce().ToLongRunningTask().ConfigureAwait(false);
		} catch (Exception e) {
			Msg.ShowError(" " + e);
			await Connect(true).ConfigureAwait(false);

			return false;
		}

		if (string.IsNullOrEmpty(callback.Nonce)) {
			await Connect(true).ConfigureAwait(false);

			return false;
		}

		await Connect(true).ConfigureAwait(false);

		return false;
	}
	
	internal async Task Start() {
		if (KeepRunning)
		{
			return;
		}

		KeepRunning = true;
		Utilities.InBackground(HandleCallbacks, true);

		if (!HasMobileAuthenticator)
		{
			string mobileAuthenticatorFilePath = $@"D:\Game\SDA\maFiles\{user.SteamId}.maFile";

			if (string.IsNullOrEmpty(mobileAuthenticatorFilePath))
			{
				Msg.ShowError(mobileAuthenticatorFilePath);

				return;
			}

			if (File.Exists(mobileAuthenticatorFilePath))
			{
				await ImportAuthenticatorFromFile(mobileAuthenticatorFilePath).ConfigureAwait(false);
			}
		}
		
		string keysToRedeemFilePath = GetFilePath(EFileType.KeysToRedeem);

		if (string.IsNullOrEmpty(keysToRedeemFilePath)) {
			Msg.ShowError(keysToRedeemFilePath);

			return;
		}

		if (File.Exists(keysToRedeemFilePath)) {
			await ImportKeysToRedeem(keysToRedeemFilePath).ConfigureAwait(false);
		}

		await Connect().ConfigureAwait(false);
	}

	internal void Stop(bool skipShutdownEvent = false) {
		if (!KeepRunning) {
			return;
		}

		KeepRunning = false;
		Msg.ShowInfo("Bot Stopping");

		if (SteamClient.IsConnected) {
			Disconnect();
		}
	}
	
	internal async Task ImportKeysToRedeem(string filePath) {
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
			throw new ArgumentNullException(nameof(filePath));
		}

		try {
			OrderedDictionary gamesToRedeemInBackground = new();

			using (StreamReader reader = new(filePath)) {
				while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line) {
					if (line.Length == 0) {
						continue;
					}

					// Valid formats:
					// Key (name will be the same as key and replaced from redemption result, if possible)
					// Name + Key (user provides both, if name is equal to key, above logic is used, otherwise name is kept)
					// Name + <Ignored> + Key (BGR output format, we include extra properties in the middle, those are ignored during import)
					string[] parsedArgs = line.Split(DefaultBackgroundKeysRedeemerSeparator, StringSplitOptions.RemoveEmptyEntries);

					if (parsedArgs.Length < 1) {
						continue;
					}

					string name = parsedArgs[0];
					string key = parsedArgs[^1];

					gamesToRedeemInBackground[key] = name;
				}
			}

			File.Delete(filePath);
		} catch (Exception e) {
			Msg.ShowError(e.ToString());
		}
	}
	
	internal static IOrderedDictionary ValidateGamesToRedeemInBackground(IOrderedDictionary gamesToRedeemInBackground) {
		if ((gamesToRedeemInBackground == null) || (gamesToRedeemInBackground.Count == 0)) {
			throw new ArgumentNullException(nameof(gamesToRedeemInBackground));
		}

		HashSet<object> invalidKeys = new();

		foreach (DictionaryEntry game in gamesToRedeemInBackground) {
			bool invalid = false;

			string? key = game.Key as string;

			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			if (string.IsNullOrEmpty(key)) {
				invalid = true;
			} else if (!Utilities.IsValidCdKey(key!)) {
				invalid = true;
			}

			string? name = game.Value as string;

			if (string.IsNullOrEmpty(name)) {
				invalid = true;
			}

			if (invalid && (key != null)) {
				invalidKeys.Add(key);
			}
		}

		if (invalidKeys.Count > 0) {
			foreach (string invalidKey in invalidKeys) {
				gamesToRedeemInBackground.Remove(invalidKey);
			}
		}

		return gamesToRedeemInBackground;
	}

	private static async Task LimitLoginRequestsAsync()
	{
		if (LoginSemaphore == null)
		{
			return;
		}

		if (LoginRateLimitingSemaphore == null)
		{
			return;
		}

		byte loginLimiterDelay = 10;

		if (loginLimiterDelay == 0)
		{
			await LoginRateLimitingSemaphore.WaitAsync().ConfigureAwait(false);
			LoginRateLimitingSemaphore.Release();

			return;
		}

		await LoginSemaphore.WaitAsync().ConfigureAwait(false);

		try
		{
			await LoginRateLimitingSemaphore.WaitAsync().ConfigureAwait(false);
			LoginRateLimitingSemaphore.Release();
		}
		finally
		{
			await Task.Delay(loginLimiterDelay * 1000).ConfigureAwait(false);
					LoginSemaphore.Release();
		}
	}
	
	private async Task Connect(bool force = false) {
		if (!force && (!KeepRunning || SteamClient.IsConnected))
		{
			return;
		}

		await LimitLoginRequestsAsync().ConfigureAwait(false);

		if (!force && (!KeepRunning || SteamClient.IsConnected))
		{
			return;
		}

		SteamClient.Connect();
	}
	
	private async Task ImportAuthenticatorFromFile(string maFilePath) {
		if (HasMobileAuthenticator || !File.Exists(maFilePath)) {
			return;
		}
		
		try {
			string json = await File.ReadAllTextAsync(maFilePath).ConfigureAwait(false);

			if (string.IsNullOrEmpty(json)) {
				return;
			}

			MobileAuthenticator? authenticator = JsonConvert.DeserializeObject<MobileAuthenticator>(json);

			if (authenticator == null) {
				Msg.ShowError(" " + authenticator);

				return;
			}

			if (!TryImportAuthenticator(authenticator)) {
				return;
			}

			File.Delete(maFilePath);
		} catch (Exception e) {
			Msg.ShowError(e.ToString());
		}
	}
	
	internal bool TryImportAuthenticator(MobileAuthenticator authenticator) {
		ArgumentNullException.ThrowIfNull(authenticator);

		if (HasMobileAuthenticator) {
			return false;
		}

		authenticator.Init(this);
		BotDatabase.MobileAuthenticator = authenticator;
		
		return true;
	}

	private async Task Destroy(bool force = false) {
		if (Bots == null) {
			throw new InvalidOperationException(nameof(Bots));
		}

		if (KeepRunning) {
			if (!force) {
				Stop();
			} else {
				// Stop() will most likely block due to connection freeze, don't wait for it
				Utilities.InBackground(() => Stop());
			}
		}

		Bots.TryRemove(BotName, out _);
	}
	
	private void StopConnectionFailureTimer() {
		if (ConnectionFailureTimer == null) {
			return;
		}

		ConnectionFailureTimer.Dispose();
		ConnectionFailureTimer = null;
	}
	
	private void Disconnect() {
		StopConnectionFailureTimer();
		SteamClient.Disconnect();
	}

	private async Task HandleCallbacks() {
		if (!await CallbackSemaphore.WaitAsync(CallbackSleep).ConfigureAwait(false)) {
			return;
		}

		try {
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(CallbackSleep);

			while (KeepRunning || SteamClient.IsConnected) {
				CallbackManager.RunWaitAllCallbacks(timeSpan);
			}
		} catch (Exception e) {
			Msg.ShowError(" " + e);
		} finally {
			CallbackSemaphore.Release();
		}
	}

	
	private async void OnConnected(SteamClient.ConnectedCallback callback) {
		
		SteamUser.LogOnDetails logOnDetails = new() {
			Password = user.Password,
			TwoFactorCode = await SteamCode.SteamCodeCreate(user),
			Username = user.Login
		};

		SteamUser.LogOn(logOnDetails);
	}
	
	private void StopPlayingWasBlockedTimer() {
		if (PlayingWasBlockedTimer == null) {
			return;
		}

		PlayingWasBlockedTimer.Dispose();
		PlayingWasBlockedTimer = null;
	}

	private async void OnDisconnected(SteamClient.DisconnectedCallback callback) {
		ArgumentNullException.ThrowIfNull(callback);

		

		EResult lastLogOnResult = LastLogOnResult;
		LastLogOnResult = EResult.Invalid;
		HeartBeatFailures = 0;
		StopConnectionFailureTimer();
		StopPlayingWasBlockedTimer();

		WebHandler.OnDisconnected();

		FirstTradeSent = false;
		OwnedPackageIDs = ImmutableDictionary<uint, (EPaymentMethod PaymentMethod, DateTime TimeCreated)>.Empty;

		// If we initiated disconnect, do not attempt to reconnect
		if (callback.UserInitiated && !ReconnectOnUserInitiated) {
			return;
		}

		switch (lastLogOnResult) {
			case EResult.AccountDisabled:
				// Do not attempt to reconnect, those failures are permanent
				return;
			default:
				// Generic delay before retrying
				await Task.Delay(5000).ConfigureAwait(false);

				break;
		}

		if (!KeepRunning || SteamClient.IsConnected) {
			return;
		}
		
		await Connect().ConfigureAwait(false);
	}

	private void OnLoggedOff(SteamUser.LoggedOffCallback callback) {
		ArgumentNullException.ThrowIfNull(callback);

		LastLogOnResult = callback.Result;

		switch (callback.Result) {
			case EResult.LoggedInElsewhere:
				// This result directly indicates that playing was blocked when we got (forcefully) disconnected
				PlayingWasBlocked = true;

				break;
			case EResult.LogonSessionReplaced:
				DateTime now = DateTime.UtcNow;

				if (now.Subtract(LastLogonSessionReplaced).TotalHours < 1) {
					Stop();

					return;
				}

				LastLogonSessionReplaced = now;

				break;
		}

		ReconnectOnUserInitiated = true;
		SteamClient.Disconnect();
	}
	
	private void ResetPlayingWasBlockedWithTimer(object? state = null) {
		PlayingWasBlocked = false;
		StopPlayingWasBlockedTimer();
	}

	private async void OnLoggedOn(SteamUser.LoggedOnCallback callback)
	{
		switch (callback.Result)
		{
			case EResult.AccountDisabled:
				// Those failures are permanent, we should Stop() the bot if any of those happen
				Stop();

				break;
			case EResult.AccountLogonDenied:
			case EResult.InvalidLoginAuthCode:
				Stop();
				break;
			case EResult.OK:

                   
				WebHandler.OnVanityURLChanged(callback.VanityURL);

				if (!await WebHandler.Init(user.SteamId, SteamClient.Universe, callback.WebAPIUserNonce ?? throw new InvalidOperationException(nameof(callback.WebAPIUserNonce))).ConfigureAwait(false))
				{
					if (!await RefreshSession().ConfigureAwait(false))
					{
						break;
					}
				}

				break;
			case EResult.AccountLoginDeniedNeedTwoFactor:
			case EResult.InvalidPassword:
			case EResult.NoConnection:
			case EResult.PasswordRequiredToKickSession: // Not sure about this one, it seems to be just generic "try again"? #694
			case EResult.RateLimitExceeded:
			case EResult.ServiceUnavailable:
			case EResult.Timeout:
			case EResult.TryAnotherCM:
			case EResult.TwoFactorCodeMismatch:
				Stop();
				break;
			default:
				// Unexpected result, shutdown immediately
				Stop();

				break;
		}
	}
	
	internal void RequestPersonaStateUpdate() {
		if (!IsConnectedAndLoggedOn) {
			return;
		}

		SteamFriends.RequestFriendInfo(SteamID, EClientPersonaStateFlag.PlayerName | EClientPersonaStateFlag.Presence);
	}

	private async void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback) {
		ArgumentNullException.ThrowIfNull(callback);

		string sentryFilePath = GetFilePath(EFileType.SentryFile);

		if (string.IsNullOrEmpty(sentryFilePath)) {
			Msg.ShowError(" " + sentryFilePath);

			return;
		}

		long fileSize;
		byte[] sentryHash;

		try {
#pragma warning disable CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose
			FileStream fileStream = File.Open(sentryFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
#pragma warning restore CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose

			await using (fileStream.ConfigureAwait(false)) {
				fileStream.Seek(callback.Offset, SeekOrigin.Begin);

				await fileStream.WriteAsync(callback.Data.AsMemory(0, callback.BytesToWrite)).ConfigureAwait(false);

				fileSize = fileStream.Length;
				fileStream.Seek(0, SeekOrigin.Begin);

#pragma warning disable CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms
				using SHA1 hashAlgorithm = SHA1.Create();

				sentryHash = await hashAlgorithm.ComputeHashAsync(fileStream).ConfigureAwait(false);
#pragma warning restore CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms
			}
		} catch (Exception e) {
			Msg.ShowError(" " + e);

			try {
				File.Delete(sentryFilePath);
			} catch {
				// Ignored, we can only try to delete faulted file at best
			}

			return;
		}

		// Inform the steam servers that we're accepting this sentry file
		SteamUser.SendMachineAuthResponse(
			new SteamUser.MachineAuthDetails {
				BytesWritten = callback.BytesToWrite,
				FileName = callback.FileName,
				FileSize = (int) fileSize,
				JobID = callback.JobID,
				LastError = 0,
				Offset = callback.Offset,
				OneTimePassword = callback.OneTimePassword,
				Result = EResult.OK,
				SentryFileHash = sentryHash
			}
		);
	}

	private void OnWalletUpdate(SteamUser.WalletInfoCallback callback) {
		ArgumentNullException.ThrowIfNull(callback);

		WalletBalance = callback.LongBalance;
		WalletCurrency = callback.Currency;
	}

	/*private (bool IsSteamParentalEnabled, string? SteamParentalCode) ValidateSteamParental(ParentalSettings settings, string? steamParentalCode = null, bool allowGeneration = true) {
		ArgumentNullException.ThrowIfNull(settings);

		if (!settings.is_enabled) {
			return (false, null);
		}

		if (settings.passwordhash.Length > byte.MaxValue) {
			throw new ArgumentOutOfRangeException(nameof(settings));
		}

		ArchiCryptoHelper.EHashingMethod steamParentalHashingMethod;

		switch (settings.passwordhashtype) {
			case 4:
				steamParentalHashingMethod = ArchiCryptoHelper.EHashingMethod.Pbkdf2;

				break;
			case 6:
				steamParentalHashingMethod = ArchiCryptoHelper.EHashingMethod.SCrypt;

				break;
			default:
				return (true, null);
		}

		if (!string.IsNullOrEmpty(steamParentalCode)) {
			byte i = 0;

			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			byte[] password = new byte[steamParentalCode!.Length];

			foreach (char character in steamParentalCode.TakeWhile(static character => character is >= '0' and <= '9')) {
				password[i++] = (byte) character;
			}

			if (i >= steamParentalCode.Length) {
				byte[] passwordHash = ArchiCryptoHelper.Hash(password, settings.salt, (byte) settings.passwordhash.Length, steamParentalHashingMethod);

				if (passwordHash.SequenceEqual(settings.passwordhash)) {
					return (true, steamParentalCode);
				}
			}
		}

		if (!allowGeneration) {
			return (true, null);
		}
		
		steamParentalCode = ArchiCryptoHelper.RecoverSteamParentalCode(settings.passwordhash, settings.salt, steamParentalHashingMethod);


		return (true, steamParentalCode);
	}*/

	public enum EFileType : byte {
		Config,
		Database,
		KeysToRedeem,
		KeysToRedeemUnused,
		KeysToRedeemUsed,
		MobileAuthenticator,
		SentryFile
	}
}