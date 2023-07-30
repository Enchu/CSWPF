using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSWPF.Steam.Security;
using CSWPF.Web;
using JetBrains.Annotations;
using Microsoft.VisualBasic;
using SteamKit2;

namespace CSWPF.Steam.Interaction;

public sealed class Actions: IAsyncDisposable, IDisposable
{
	private static readonly SemaphoreSlim GiftCardsSemaphore = new(1, 1);

	private readonly Bot Bot;
	private readonly SemaphoreSlim TradingSemaphore = new(1, 1);
	internal Actions(Bot bot) => Bot = bot ?? throw new ArgumentNullException(nameof(bot));
	
	public void Dispose() {
		TradingSemaphore.Dispose();
	}

	public async ValueTask DisposeAsync() {
		// Those are objects that are always being created if constructor doesn't throw exception
		TradingSemaphore.Dispose();
	}
	
	public async Task<(bool Success, IReadOnlyCollection<Confirmation>? HandledConfirmations)> HandleTwoFactorAuthenticationConfirmations(bool accept, Confirmation.EConfirmationType? acceptedType = null, IReadOnlyCollection<ulong>? acceptedCreatorIDs = null, bool waitIfNeeded = false) {
		if (Bot.MobileAuthenticator == null) {
			return (false, null);
		}

		if (!Bot.IsConnectedAndLoggedOn) {
			return (false, null);
		}

		Dictionary<ulong, Confirmation>? handledConfirmations = null;

		for (byte i = 0; (i == 0) || ((i < WebBrowser.MaxTries) && waitIfNeeded); i++) {
			if (i > 0) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			ImmutableHashSet<Confirmation>? confirmations = await Bot.MobileAuthenticator.GetConfirmations().ConfigureAwait(false);

			if ((confirmations == null) || (confirmations.Count == 0)) {
				continue;
			}

			HashSet<Confirmation> remainingConfirmations = confirmations.ToHashSet();

			if (acceptedType.HasValue) {
				if (remainingConfirmations.RemoveWhere(confirmation => confirmation.ConfirmationType != acceptedType.Value) > 0) {
					if (remainingConfirmations.Count == 0) {
						continue;
					}
				}
			}

			if (acceptedCreatorIDs?.Count > 0) {
				if (remainingConfirmations.RemoveWhere(confirmation => !acceptedCreatorIDs.Contains(confirmation.CreatorID)) > 0) {
					if (remainingConfirmations.Count == 0) {
						continue;
					}
				}
			}

			if (!await Bot.MobileAuthenticator.HandleConfirmations(remainingConfirmations, accept).ConfigureAwait(false)) {
				return (false, handledConfirmations?.Values);
			}

			handledConfirmations ??= new Dictionary<ulong, Confirmation>();

			foreach (Confirmation? confirmation in remainingConfirmations) {
				handledConfirmations[confirmation.CreatorID] = confirmation;
			}

			// We've accepted *something*, if caller didn't specify the IDs, that's enough for us
			if ((acceptedCreatorIDs == null) || (acceptedCreatorIDs.Count == 0)) {
				return (true, handledConfirmations.Values);
			}

			// If he did, check if we've already found everything we were supposed to
			if ((handledConfirmations.Count >= acceptedCreatorIDs.Count) && acceptedCreatorIDs.All(handledConfirmations.ContainsKey)) {
				return (true, handledConfirmations.Values);
			}
		}

		// If we've reached this point, then it's a failure for waitIfNeeded, and success otherwise
		return (!waitIfNeeded, handledConfirmations?.Values);
	}

}