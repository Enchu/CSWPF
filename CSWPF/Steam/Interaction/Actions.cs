using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSWPF.Steam.Security;
using CSWPF.Web;
using JetBrains.Annotations;
using SteamKit2;

namespace CSWPF.Steam.Interaction;

public sealed class Actions
{
	public async Task<(bool Success, IReadOnlyCollection<Confirmation>? HandledConfirmations, string Message)> HandleTwoFactorAuthenticationConfirmations(bool accept, Confirmation.EType? acceptedType = null, IReadOnlyCollection<ulong>? acceptedCreatorIDs = null, bool waitIfNeeded = false) {
		if (Bot.BotDatabase.MobileAuthenticator == null) {
			return (false, null,"");
		}

		if (!Bot.IsConnectedAndLoggedOn) {
			return (false, null, "");
		}

		Dictionary<ulong, Confirmation>? handledConfirmations = null;

		for (byte i = 0; (i == 0) || ((i < WebBrowser.MaxTries) && waitIfNeeded); i++) {
			if (i > 0) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			HashSet<Confirmation>? confirmations = await Bot.BotDatabase.MobileAuthenticator.GetConfirmations().ConfigureAwait(false);

			if ((confirmations == null) || (confirmations.Count == 0)) {
				continue;
			}

			if (acceptedType.HasValue) {
				if (confirmations.RemoveWhere(confirmation => confirmation.Type != acceptedType.Value) > 0) {
					if (confirmations.Count == 0) {
						continue;
					}
				}
			}

			if (acceptedCreatorIDs?.Count > 0) {
				if (confirmations.RemoveWhere(confirmation => !acceptedCreatorIDs.Contains(confirmation.Creator)) > 0) {
					if (confirmations.Count == 0) {
						continue;
					}
				}
			}

			if (!await Bot.BotDatabase.MobileAuthenticator.HandleConfirmations(confirmations, accept).ConfigureAwait(false)) {
				return (false, handledConfirmations?.Values, "WarningFailed");
			}

			handledConfirmations ??= new Dictionary<ulong, Confirmation>();

			foreach (Confirmation? confirmation in confirmations) {
				handledConfirmations[confirmation.Creator] = confirmation;
			}

			// We've accepted *something*, if caller didn't specify the IDs, that's enough for us
			if ((acceptedCreatorIDs == null) || (acceptedCreatorIDs.Count == 0)) {
				return (true, handledConfirmations.Values, string.Format(CultureInfo.CurrentCulture, "BotHandledConfirmations", handledConfirmations.Count));
			}

			// If he did, check if we've already found everything we were supposed to
			if ((handledConfirmations.Count >= acceptedCreatorIDs.Count) && acceptedCreatorIDs.All(handledConfirmations.ContainsKey)) {
				return (true, handledConfirmations.Values, string.Format(CultureInfo.CurrentCulture, "BotHandledConfirmations", handledConfirmations.Count));
			}
		}

		// If we've reached this point, then it's a failure for waitIfNeeded, and success otherwise
		return (!waitIfNeeded, handledConfirmations?.Values, !waitIfNeeded ? string.Format(CultureInfo.CurrentCulture, "BotHandledConfirmations", handledConfirmations?.Count ?? 0) : string.Format(CultureInfo.CurrentCulture, "Strings.ErrorRequestFailedTooManyTimes", WebBrowser.MaxTries));
	}

}