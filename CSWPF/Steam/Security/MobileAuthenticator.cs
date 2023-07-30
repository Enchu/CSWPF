using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using CSWPF.Steam.Data;
using CSWPF.Utils;
using CSWPF.Web;
using CSWPF.Web.Assistance;
using CSWPF.Web.Core;
using CSWPF.Web.Responses;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace CSWPF.Steam.Security;

public sealed class MobileAuthenticator : IDisposable {
	internal const byte BackupCodeDigits = 7;
	internal const byte CodeDigits = 5;

	private const byte CodeInterval = 30;

	// For how many minutes we can assume that SteamTimeDifference is correct
	private const byte SteamTimeTTL = 15;

	internal static readonly ImmutableSortedSet<char> CodeCharacters = ImmutableSortedSet.Create('2', '3', '4', '5', '6', '7', '8', '9', 'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'M', 'N', 'P', 'Q', 'R', 'T', 'V', 'W', 'X', 'Y');

	private static readonly SemaphoreSlim TimeSemaphore = new(1, 1);

	private static DateTime LastSteamTimeCheck;
	private static int? SteamTimeDifference;

	private readonly Cacheable<string> CachedDeviceID;

	[JsonProperty("identity_secret", Required = Required.Always)]
	private readonly string IdentitySecret = "";

	[JsonProperty("shared_secret", Required = Required.Always)]
	private readonly string SharedSecret = "";

	private Bot? bot;

	[JsonConstructor]
	private MobileAuthenticator() => CachedDeviceID = new Cacheable<string>(ResolveDeviceID);

	public void Dispose() => CachedDeviceID.Dispose();

	internal async Task<string?> GenerateToken() {
		if (bot == null) {
			throw new InvalidOperationException(nameof(bot));
		}

		ulong time = await GetSteamTime().ConfigureAwait(false);

		if (time == 0) {
			throw new InvalidOperationException(nameof(time));
		}

		return GenerateTokenForTime(time);
	}

	internal async Task<ImmutableHashSet<Confirmation>?> GetConfirmations() {
		if (bot == null) {
			throw new InvalidOperationException(nameof(Bot));
		}

		(_, string? deviceID) = await CachedDeviceID.GetValue(ECacheFallback.SuccessPreviously).ConfigureAwait(false);

		if (string.IsNullOrEmpty(deviceID)) {
			return null;
		}

		await LimitConfirmationsRequestsAsync().ConfigureAwait(false);

		ulong time = await GetSteamTime().ConfigureAwait(false);

		if (time == 0) {
			throw new InvalidOperationException(nameof(time));
		}

		string? confirmationHash = GenerateConfirmationHash(time, "conf");

		if (string.IsNullOrEmpty(confirmationHash)) {
			return null;
		}

		// ReSharper disable RedundantSuppressNullableWarningExpression - required for .NET Framework
		ConfirmationsResponse? response = await bot.WebHandler.GetConfirmations(deviceID!, confirmationHash!, time).ConfigureAwait(false);

		if (response?.Success != true) {
			return null;
		}

		foreach (Confirmation? confirmation in response.Confirmations.Where(static confirmation => (confirmation.ConfirmationType == Confirmation.EConfirmationType.Unknown) || !Enum.IsDefined(confirmation.ConfirmationType))) {
		}

		return response.Confirmations;
	}

	internal async Task<bool> HandleConfirmations(IReadOnlyCollection<Confirmation> confirmations, bool accept) {
		if ((confirmations == null) || (confirmations.Count == 0)) {
			throw new ArgumentNullException(nameof(confirmations));
		}

		if (bot == null) {
			throw new InvalidOperationException(nameof(Bot));
		}

		(_, string? deviceID) = await CachedDeviceID.GetValue(ECacheFallback.SuccessPreviously).ConfigureAwait(false);

		if (string.IsNullOrEmpty(deviceID)) {
			
			return false;
		}

		ulong time = await GetSteamTime().ConfigureAwait(false);

		if (time == 0) {
			throw new InvalidOperationException(nameof(time));
		}

		string? confirmationHash = GenerateConfirmationHash(time, "conf");

		if (string.IsNullOrEmpty(confirmationHash)) {
			
			return false;
		}

		// ReSharper disable RedundantSuppressNullableWarningExpression - required for .NET Framework
		bool? result = await bot.WebHandler.HandleConfirmations(deviceID!, confirmationHash!, time, confirmations, accept).ConfigureAwait(false);

		// ReSharper restore RedundantSuppressNullableWarningExpression - required for .NET Framework

		if (!result.HasValue) {
			// Request timed out
			return false;
		}

		if (result.Value) {
			// Request succeeded
			return true;
		}

		// Our multi request failed, this is almost always Steam issue that happens randomly
		// In this case, we'll accept all pending confirmations one-by-one, synchronously (as Steam can't handle them in parallel)
		// We totally ignore actual result returned by those calls, abort only if request timed out
		foreach (Confirmation confirmation in confirmations) {
			// ReSharper disable RedundantSuppressNullableWarningExpression - required for .NET Framework
			bool? confirmationResult = await bot.WebHandler.HandleConfirmation(deviceID!, confirmationHash!, time, confirmation.ID, confirmation.Nonce, accept).ConfigureAwait(false);

			// ReSharper restore RedundantSuppressNullableWarningExpression - required for .NET Framework

			if (!confirmationResult.HasValue) {
				return false;
			}
		}

		return true;
	}

	internal void Init(Bot bot) => this.bot = bot ?? throw new ArgumentNullException(nameof(bot));

	internal static async Task ResetSteamTimeDifference() {
		if ((SteamTimeDifference == null) && (LastSteamTimeCheck == DateTime.MinValue)) {
			return;
		}

		if (!await TimeSemaphore.WaitAsync(0).ConfigureAwait(false)) {
			// Resolve or reset is already in-progress
			return;
		}

		try {
			if ((SteamTimeDifference == null) && (LastSteamTimeCheck == DateTime.MinValue)) {
				return;
			}

			SteamTimeDifference = null;
			LastSteamTimeCheck = DateTime.MinValue;
		} finally {
			TimeSemaphore.Release();
		}
	}

	private string? GenerateConfirmationHash(ulong time, string? tag = null) {
		if (time == 0) {
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		if (bot == null) {
			throw new InvalidOperationException(nameof(bot));
		}

		if (string.IsNullOrEmpty(IdentitySecret)) {
			throw new InvalidOperationException(nameof(IdentitySecret));
		}

		byte[] identitySecret;

		try {
			identitySecret = Convert.FromBase64String(IdentitySecret);
		} catch (FormatException e) {
			Msg.ShowError(e.ToString());
			Msg.ShowError( nameof(IdentitySecret));

			return null;
		}

		byte bufferSize = 8;

		if (!string.IsNullOrEmpty(tag)) {
			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			bufferSize += (byte) Math.Min(32, tag!.Length);
		}

		byte[] timeArray = BitConverter.GetBytes(time);

		if (BitConverter.IsLittleEndian) {
			Array.Reverse(timeArray);
		}

		byte[] buffer = new byte[bufferSize];

		Array.Copy(timeArray, buffer, 8);

		if (!string.IsNullOrEmpty(tag)) {
			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			Array.Copy(Encoding.UTF8.GetBytes(tag!), 0, buffer, 8, bufferSize - 8);
		}

#pragma warning disable CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms
		byte[] hash = HMACSHA1.HashData(identitySecret, buffer);
#pragma warning restore CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms

		return Convert.ToBase64String(hash);
	}

	private string? GenerateTokenForTime(ulong time) {
		if (time == 0) {
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		if (bot == null) {
			throw new InvalidOperationException(nameof(bot));
		}

		if (string.IsNullOrEmpty(SharedSecret)) {
			throw new InvalidOperationException(nameof(SharedSecret));
		}

		byte[] sharedSecret;

		try {
			sharedSecret = Convert.FromBase64String(SharedSecret);
		} catch (FormatException e) {
			Msg.ShowError(e.ToString());
			Msg.ShowError( nameof(SharedSecret));

			return null;
		}

		byte[] timeArray = BitConverter.GetBytes(time / CodeInterval);

		if (BitConverter.IsLittleEndian) {
			Array.Reverse(timeArray);
		}

#pragma warning disable CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms
		byte[] hash = HMACSHA1.HashData(sharedSecret, timeArray);
#pragma warning restore CA5350 // This is actually a fair warning, but there is nothing we can do about Steam using weak cryptographic algorithms

		// The last 4 bits of the mac say where the code starts
		int start = hash[^1] & 0x0f;

		// Extract those 4 bytes
		byte[] bytes = new byte[4];

		Array.Copy(hash, start, bytes, 0, 4);

		if (BitConverter.IsLittleEndian) {
			Array.Reverse(bytes);
		}

		// Build the alphanumeric code
		uint fullCode = BitConverter.ToUInt32(bytes, 0) & 0x7fffffff;

		// ReSharper disable once BuiltInTypeReferenceStyleForMemberAccess - required for .NET Framework
		return String.Create(
			CodeDigits, fullCode, static (buffer, state) => {
				for (byte i = 0; i < CodeDigits; i++) {
					buffer[i] = CodeCharacters[(byte) (state % CodeCharacters.Count)];
					state /= (byte) CodeCharacters.Count;
				}
			}
		);
	}

	private async Task<ulong> GetSteamTime() {
		if (bot == null) {
			throw new InvalidOperationException(nameof(bot));
		}

		int? steamTimeDifference = SteamTimeDifference;

		if (steamTimeDifference.HasValue && (DateTime.UtcNow.Subtract(LastSteamTimeCheck).TotalMinutes < SteamTimeTTL)) {
			return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
		}

		await TimeSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			steamTimeDifference = SteamTimeDifference;

			if (steamTimeDifference.HasValue && (DateTime.UtcNow.Subtract(LastSteamTimeCheck).TotalMinutes < SteamTimeTTL)) {
				return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
			}

			ulong serverTime = await WebHandler.GetServerTime().ConfigureAwait(false);

			if (serverTime == 0) {
				return Utilities.GetUnixTime();
			}

			// We assume that the difference between times will be within int range, therefore we accept underflow here (for subtraction), and since we cast that result to int afterwards, we also accept overflow for the cast itself
			steamTimeDifference = unchecked((int) (serverTime - Utilities.GetUnixTime()));

			SteamTimeDifference = steamTimeDifference;
			LastSteamTimeCheck = DateTime.UtcNow;
		} finally {
			TimeSemaphore.Release();
		}

		return Utilities.MathAdd(Utilities.GetUnixTime(), steamTimeDifference.Value);
	}
	
	//internal static ICrossProcessSemaphore? ConfirmationsSemaphore { get; private set; }
	public static GlobalConfig? GlobalConfig { get; internal set; }
	
	private static async Task LimitConfirmationsRequestsAsync() {
		/*if (ConfirmationsSemaphore == null) {
			throw new InvalidOperationException(nameof(ConfirmationsSemaphore));
		}*/

		byte confirmationsLimiterDelay = GlobalConfig?.ConfirmationsLimiterDelay ?? GlobalConfig.DefaultConfirmationsLimiterDelay;

		if (confirmationsLimiterDelay == 0) {
			return;
		}

		//await ConfirmationsSemaphore.WaitAsync().ConfigureAwait(false);

		Utilities.InBackground(
			async () => {
				await Task.Delay(confirmationsLimiterDelay * 1000).ConfigureAwait(false);
				//ConfirmationsSemaphore.Release();
			}
		);
	}

	private async Task<(bool Success, string? Result)> ResolveDeviceID() {
		if (bot == null) {
			throw new InvalidOperationException(nameof(bot));
		}

		string? deviceID = await bot.ArchiHandler.GetTwoFactorDeviceIdentifier(bot.user.SteamID).ConfigureAwait(false);

		if (string.IsNullOrEmpty(deviceID)) {
			return (false, null);
		}

		return (true, deviceID);
	}
}
