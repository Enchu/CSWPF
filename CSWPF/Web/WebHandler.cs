using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using AngleSharp.Dom;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Steam;
using CSWPF.Steam.Data;
using CSWPF.Utils;
using CSWPF.Web.Core;
using CSWPF.Web.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamAuth;
using SteamKit2;

namespace CSWPF.Web;

public sealed class WebHandler : IDisposable {
	private const string EconService = "IEconService";
	private const string LoyaltyRewardsService = "ILoyaltyRewardsService";
	private const ushort MaxItemsInSingleInventoryRequest = 5000;
	private const byte MinimumSessionValidityInSeconds = 10;
	private const string SteamAppsService = "ISteamApps";
	private const string SteamUserAuthService = "ISteamUserAuth";
	private const string TwoFactorService = "ITwoFactorService";
	
	public static Uri SteamCommunityURL => new("https://steamcommunity.com");
	public static Uri SteamHelpURL => new("https://help.steampowered.com");
	public static Uri SteamStoreURL => new("https://store.steampowered.com");
	private static ushort WebLimiterDelay => GlobalConfig.DefaultWebLimiterDelay;
	public Cacheable<string> CachedAccessToken { get; }
	public static Cacheable<string> CachedApiKey { get; set; }
	public WebBrowser WebBrowser { get; }

	private static Bot Bot;
	private readonly SemaphoreSlim SessionSemaphore = new(1, 1);
	public static SemaphoreSlim OpenConnectionsSemaphore = new(1, 1);
	public static SemaphoreSlim RateLimitingSemaphore = new(1, 1);
	
	internal static ICrossProcessSemaphore? InventorySemaphore { get; private set; }

	private bool Initialized;
	private DateTime LastSessionCheck;
	private bool MarkingInventoryScheduled;
	private DateTime SessionValidUntil;
	private string? VanityURL;

	internal WebHandler(Bot bot) {
		Bot = bot ?? throw new ArgumentNullException(nameof(bot));

		CachedApiKey = new Cacheable<string>(ResolveApiKey);
		CachedAccessToken = new Cacheable<string>(ResolveAccessToken);

		WebBrowser = new WebBrowser();
	}

	public void Dispose() {
		CachedApiKey.Dispose();
		CachedAccessToken.Dispose();
		SessionSemaphore.Dispose();
		WebBrowser.Dispose();
	}

	public async Task<string?> GetAbsoluteProfileURL(bool waitForInitialization = true) {
		if (waitForInitialization && !Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		return string.IsNullOrEmpty(VanityURL) ? $"/profiles/{Bot.SteamID}" : $"/id/{VanityURL}";
	}

	public async  Task<List<InventoryResponseCS.Asset>> GetInventoryAsync(ulong steamID = 0, uint appID = AssetCS.SteamAppID, ulong contextID = AssetCS.SteamCommunityContextID)
	{
		ulong startAssetID = 0;
		
		HashSet<ulong>? assetIDs = null;

		int rateLimitingDelay = (GlobalConfig.DefaultInventoryLimiterDelay) * 1000;
		
			Uri request = new(SteamCommunityURL, $"/inventory/{steamID}/{appID}/{contextID}?l=english&count={MaxItemsInSingleInventoryRequest}{(startAssetID > 0 ? $"&start_assetid={startAssetID}" : "")}");

			ObjectResponse<InventoryResponseCS>? response = null;
			try
			{
				for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++)
				{
					if ((i > 0) && (rateLimitingDelay > 0))
					{
						await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
					}

					response = await UrlGetToJsonObjectWithSession<InventoryResponseCS>(request,
						requestOptions: WebBrowser.ERequestOptions.ReturnClientErrors |
						                WebBrowser.ERequestOptions.ReturnServerErrors |
						                WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors,
						rateLimitingDelay: rateLimitingDelay).ConfigureAwait(false);

					if (response == null || Helper.IsClientErrorCode(response.StatusCode))
					{
						return null;
					}

					if (Helper.IsServerErrorCode(response.StatusCode))
					{
						return null;
					}
				}
			}
			catch (Exception ex)
			{
				Msg.ShowError("" + ex.ToString());
			}
			finally {
				if (rateLimitingDelay == 0) {
					InventorySemaphore.Release();
				} else {
					Utilities.InBackground(
						async () => {
							await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
							InventorySemaphore.Release();
						}
					);
				}
			}

			if (response?.Content == null)
            {
                return null;
            }

            if (response.Content.Result is not EResult.OK)
            {
                return null;
            }

            if (response.Content.TotalInventoryCount == 0)
            {
                return null;
            }

            if (response.Content.TotalInventoryCount > Array.MaxLength)
            {
                return null;
            }

            assetIDs ??= new HashSet<ulong>((int)response.Content.TotalInventoryCount);

            if ((response.Content.Assets.Count() == 0) || (response.Content.Descriptions.Count() == 0))
            {
                return null;
            }

            Dictionary<(ulong ClassID, ulong InstanceID), InventoryResponseCS.Description> descriptions = new();

            List<InventoryResponseCS.Asset> AssetListStatement = new List<InventoryResponseCS.Asset>();

            foreach (InventoryResponseCS.Asset asset in response.Content.Assets)
            {
                /*if (!descriptions.TryGetValue((asset.Classid, asset.Instanceid), out InventoryResponseCS.Description? description) || !assetIDs.Add(asset.Assetid))
                {
                    continue;
                }

                asset.Tradable = description.Tradable;
                asset.Marketable = description.Marketable;
                asset.Tags = description.Tags;*/

                AssetListStatement.Add(asset);
            }
            return AssetListStatement;
	}

	public async Task<uint?> GetPointsBalance() {
		(bool success, string? accessToken) = await CachedAccessToken.GetValue().ConfigureAwait(false);

		if (!success || string.IsNullOrEmpty(accessToken)) {
			return null;
		}

		Dictionary<string, object?> arguments = new(2, StringComparer.Ordinal) {
			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			{ "access_token", accessToken! },

			// ReSharper disable once HeapView.BoxingAllocation
			{ "steamid", Bot.SteamID }
		};

		KeyValue? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (WebLimiterDelay > 0)) {
				await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
			}

			using WebAPI.AsyncInterface loyaltyRewardsService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(LoyaltyRewardsService);

			loyaltyRewardsService.Timeout = WebBrowser.Timeout;

			try {
				response = await WebLimitRequest(
					WebAPI.DefaultBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await loyaltyRewardsService.CallAsync(HttpMethod.Get, "GetSummary", args: arguments).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				Msg.ShowError("" + e);
			} catch (Exception e) {
				Msg.ShowError("" + e);
			}
		}

		if (response == null) {
			return null;
		}

		KeyValue pointsInfo = response["summary"]["points"];

		if (pointsInfo == KeyValue.Invalid) {
			return null;
		}

		uint result = pointsInfo.AsUnsignedInteger(uint.MaxValue);

		if (result == uint.MaxValue) {
			return null;
		}

		return result;
	}

	public async Task<bool?> HasValidApiKey() {
		(bool success, string? steamApiKey) = await CachedApiKey.GetValue().ConfigureAwait(false);

		return success ? !string.IsNullOrEmpty(steamApiKey) : null;
	}

	public async Task<bool> JoinGroup(ulong groupID) {
		if ((groupID == 0) || !new SteamID(groupID).IsClanAccount) {
			throw new ArgumentOutOfRangeException(nameof(groupID));
		}

		Uri request = new(SteamCommunityURL, $"/gid/{groupID}");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(2, StringComparer.Ordinal) { { "action", "join" } };

		return await UrlPostWithSession(request, data: data, session: ESession.CamelCase).ConfigureAwait(false);
	}

	public async Task<(bool Success, HashSet<ulong>? MobileTradeOfferIDs)> SendTradeOffer(ulong steamID, IReadOnlyCollection<AssetCS>? itemsToGive = null, IReadOnlyCollection<AssetCS>? itemsToReceive = null, string? token = null, bool forcedSingleOffer = false, ushort itemsPerTrade = 255) {
		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		if (((itemsToGive == null) || (itemsToGive.Count == 0)) && ((itemsToReceive == null) || (itemsToReceive.Count == 0))) {
			throw new ArgumentException($"{nameof(itemsToGive)} && {nameof(itemsToReceive)}");
		}

		if (itemsPerTrade <= 2) {
			throw new ArgumentOutOfRangeException(nameof(itemsPerTrade));
		}

		TradeOfferSendRequest singleTrade = new();
		HashSet<TradeOfferSendRequest> trades = new() { singleTrade };

		if (itemsToGive != null) {
			foreach (AssetCS itemToGive in itemsToGive) {
				if (!forcedSingleOffer && (singleTrade.ItemsToGive.Assets.Count + singleTrade.ItemsToReceive.Assets.Count >= itemsPerTrade)) {
					if (trades.Count >= 255) {
						break;
					}

					singleTrade = new TradeOfferSendRequest();
					trades.Add(singleTrade);
				}

				singleTrade.ItemsToGive.Assets.Add(itemToGive);
			}
		}

		if (itemsToReceive != null) {
			foreach (AssetCS itemToReceive in itemsToReceive) {
				if (!forcedSingleOffer && (singleTrade.ItemsToGive.Assets.Count + singleTrade.ItemsToReceive.Assets.Count >= itemsPerTrade)) {
					if (trades.Count >= 255) {
						break;
					}

					singleTrade = new TradeOfferSendRequest();
					trades.Add(singleTrade);
				}

				singleTrade.ItemsToReceive.Assets.Add(itemToReceive);
			}
		}

		Uri request = new(SteamCommunityURL, "/tradeoffer/new/send");
		Uri referer = new(SteamCommunityURL, "/tradeoffer/new");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(6, StringComparer.Ordinal) {
			{ "partner", steamID.ToString(CultureInfo.InvariantCulture) },
			{ "serverid", "1" },
			{ "trade_offer_create_params", !string.IsNullOrEmpty(token) ? new JObject { { "trade_offer_access_token", token } }.ToString(Formatting.None) : "" },
		};

		HashSet<ulong> mobileTradeOfferIDs = new(trades.Count);

		foreach (TradeOfferSendRequest trade in trades) {
			data["json_tradeoffer"] = JsonConvert.SerializeObject(trade);

			ObjectResponse<TradeOfferSendResponse>? response = null;

			for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
				response = await UrlPostToJsonObjectWithSession<TradeOfferSendResponse>(request, data: data, referer: referer, requestOptions: WebBrowser.ERequestOptions.ReturnServerErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors).ConfigureAwait(false);

				if (response == null) {
					return (false, mobileTradeOfferIDs);
				}

				if (Core.Utilities.IsServerErrorCode(response.StatusCode)) {
					if (string.IsNullOrEmpty(response.Content?.ErrorText)) {
						// This is a generic server error without a reason, try again
						response = null;

						continue;
					}

					// This is actually client error with a reason, so it doesn't make sense to retry
					// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
					return (false, mobileTradeOfferIDs);
				}
			}

			if (response?.Content == null) {
				return (false, mobileTradeOfferIDs);
			}

			if (response.Content.TradeOfferID == 0) {
				return (false, mobileTradeOfferIDs);
			}

			if (response.Content.RequiresMobileConfirmation) {
				mobileTradeOfferIDs.Add(response.Content.TradeOfferID);
			}
		}

		return (true, mobileTradeOfferIDs);
	}
	
	public async Task<HtmlDocumentResponse?> UrlGetToHtmlDocumentWithSession(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlGetToHtmlDocumentWithSession(request, headers, referer, requestOptions, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return null;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		HtmlDocumentResponse? response = await WebLimitRequest(host, async () => await WebBrowser.UrlGetToHtmlDocument(request, headers, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return null;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlGetToHtmlDocumentWithSession(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return null;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return null;
			}

			return await UrlGetToHtmlDocumentWithSession(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return response;
	}

	public async Task<ObjectResponse<T>?> UrlGetToJsonObjectWithSession<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlGetToJsonObjectWithSession<T>(request, headers, referer, requestOptions, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return null;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return default(ObjectResponse<T>?);
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		ObjectResponse<T>? response = await WebLimitRequest(host, async () => await WebBrowser.UrlGetToJsonObject<T>(request, headers, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return default(ObjectResponse<T>?);
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlGetToJsonObjectWithSession<T>(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return null;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return null;
			}

			return await UrlGetToJsonObjectWithSession<T>(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return response;
	}

	public async Task<bool> UrlHeadWithSession(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlHeadWithSession(request, headers, referer, requestOptions, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return false;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return false;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		BasicResponse? response = await WebLimitRequest(host, async () => await WebBrowser.UrlHead(request, headers, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return false;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlHeadWithSession(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return false;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return false;
			}

			return await UrlHeadWithSession(request, headers, referer, requestOptions, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return true;
	}

	public async Task<HtmlDocumentResponse?> UrlPostToHtmlDocumentWithSession(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, IDictionary<string, string>? data = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, ESession session = ESession.Lowercase, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (!Enum.IsDefined(session)) {
			throw new InvalidEnumArgumentException(nameof(session), (int) session, typeof(ESession));
		}

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlPostToHtmlDocumentWithSession(request, headers, data, referer, requestOptions, session, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return null;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		if (session != ESession.None) {
			//string? sessionID = WebBrowser.CookieContainer.GetCookieValue(host, "sessionid");
			string? sessionID = GetCookieValue(WebBrowser.CookieContainer,host, "sessionid");

			if (string.IsNullOrEmpty(sessionID)) {
				Msg.ShowError(sessionID);

				return null;
			}

			string sessionName = session switch {
				ESession.CamelCase => "sessionID",
				ESession.Lowercase => "sessionid",
				ESession.PascalCase => "SessionID",
				_ => throw new ArgumentOutOfRangeException(nameof(session))
			};

			if (data != null) {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data[sessionName] = sessionID!;
			} else {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data = new Dictionary<string, string>(1, StringComparer.Ordinal) { { sessionName, sessionID! } };
			}
		}

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		HtmlDocumentResponse? response = await WebLimitRequest(host, async () => await WebBrowser.UrlPostToHtmlDocument(request, headers, data, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return null;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlPostToHtmlDocumentWithSession(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			
			return null;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return null;
			}

			return await UrlPostToHtmlDocumentWithSession(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return response;
	}

	public static string? GetCookieValue(CookieContainer cookieContainer, Uri uri, string name) {
		ArgumentNullException.ThrowIfNull(cookieContainer);
		ArgumentNullException.ThrowIfNull(uri);

		if (string.IsNullOrEmpty(name)) {
			throw new ArgumentNullException(nameof(name));
		}

		CookieCollection cookies = cookieContainer.GetCookies(uri);
		return cookies.Count > 0 ? (from Cookie cookie in cookies where cookie.Name == name select cookie.Value).FirstOrDefault() : null;

	}

	public async Task<ObjectResponse<T>?> UrlPostToJsonObjectWithSession<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, IDictionary<string, string>? data = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, ESession session = ESession.Lowercase, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (!Enum.IsDefined(session)) {
			throw new InvalidEnumArgumentException(nameof(session), (int) session, typeof(ESession));
		}

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return null;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		if (session != ESession.None) {
			string? sessionID = GetCookieValue(WebBrowser.CookieContainer,host, "sessionid");

			if (string.IsNullOrEmpty(sessionID)) {
				return null;
			}

			string sessionName = session switch {
				ESession.CamelCase => "sessionID",
				ESession.Lowercase => "sessionid",
				ESession.PascalCase => "SessionID",
				_ => throw new ArgumentOutOfRangeException(nameof(session))
			};

			if (data != null) {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data[sessionName] = sessionID!;
			} else {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data = new Dictionary<string, string>(1, StringComparer.Ordinal) { { sessionName, sessionID! } };
			}
		}

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		ObjectResponse<T>? response = await WebLimitRequest(host, async () => await WebBrowser.UrlPostToJsonObject<T, IDictionary<string, string>>(request, headers, data, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return null;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return null;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return null;
			}

			return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return response;
	}
	
	public async Task<ObjectResponse<T>?> UrlPostToJsonObjectWithSession<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, ICollection<KeyValuePair<string, string>>? data = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, ESession session = ESession.Lowercase, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (!Enum.IsDefined(session)) {
			throw new InvalidEnumArgumentException(nameof(session), (int) session, typeof(ESession));
		}

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return null;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		if (session != ESession.None) {
			string? sessionID = GetCookieValue(WebBrowser.CookieContainer,host, "sessionid");

			if (string.IsNullOrEmpty(sessionID)) {
				return null;
			}

			string sessionName = session switch {
				ESession.CamelCase => "sessionID",
				ESession.Lowercase => "sessionid",
				ESession.PascalCase => "SessionID",
				_ => throw new ArgumentOutOfRangeException(nameof(session))
			};

			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			KeyValuePair<string, string> sessionValue = new(sessionName, sessionID!);

			if (data != null) {
				data.Remove(sessionValue);
				data.Add(sessionValue);
			} else {
				data = new List<KeyValuePair<string, string>>(1) { sessionValue };
			}
		}

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		ObjectResponse<T>? response = await WebLimitRequest(host, async () => await WebBrowser.UrlPostToJsonObject<T, ICollection<KeyValuePair<string, string>>>(request, headers, data, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return null;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return null;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return null;
			}

			return await UrlPostToJsonObjectWithSession<T>(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return response;
	}
	public async Task<bool> UrlPostWithSession(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, IDictionary<string, string>? data = null, Uri? referer = null, WebBrowser.ERequestOptions requestOptions = WebBrowser.ERequestOptions.None, ESession session = ESession.Lowercase, bool checkSessionPreemptively = true, byte maxTries = WebBrowser.MaxTries, int rateLimitingDelay = 0, bool allowSessionRefresh = true) {
		ArgumentNullException.ThrowIfNull(request);

		if (!Enum.IsDefined(session)) {
			throw new InvalidEnumArgumentException(nameof(session), (int) session, typeof(ESession));
		}

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		if (WebLimiterDelay > rateLimitingDelay) {
			rateLimitingDelay = WebLimiterDelay;
		}

		if (checkSessionPreemptively) {
			// Check session preemptively as this request might not get redirected to expiration
			bool? sessionExpired = await IsSessionExpired().ConfigureAwait(false);

			if (sessionExpired.GetValueOrDefault(true)) {
				if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
					return await UrlPostWithSession(request, headers, data, referer, requestOptions, session, true, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
				}
				return false;
			}
		} else {
			// If session refresh is already in progress, just wait for it
			await SessionSemaphore.WaitAsync().ConfigureAwait(false);
			SessionSemaphore.Release();
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return false;
			}
		}

		Uri host = new(request.GetLeftPart(UriPartial.Authority));

		if (session != ESession.None) {
			string? sessionID = GetCookieValue(WebBrowser.CookieContainer,host, "sessionid");

			if (string.IsNullOrEmpty(sessionID)) {
				return false;
			}

			string sessionName = session switch {
				ESession.CamelCase => "sessionID",
				ESession.Lowercase => "sessionid",
				ESession.PascalCase => "SessionID",
				_ => throw new ArgumentOutOfRangeException(nameof(session))
			};

			if (data != null) {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data[sessionName] = sessionID!;
			} else {
				// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
				data = new Dictionary<string, string>(1, StringComparer.Ordinal) { { sessionName, sessionID! } };
			}
		}

		// ReSharper disable once AccessToModifiedClosure - evaluated fully before returning
		BasicResponse? response = await WebLimitRequest(host, async () => await WebBrowser.UrlPost(request, headers, data, referer, requestOptions, maxTries, rateLimitingDelay).ConfigureAwait(false)).ConfigureAwait(false);

		if (response == null) {
			return false;
		}

		if (IsSessionExpiredUri(response.FinalUri)) {
			if (allowSessionRefresh && await RefreshSession().ConfigureAwait(false)) {
				return await UrlPostWithSession(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, false).ConfigureAwait(false);
			}
			return false;
		}

		// Under special brain-damaged circumstances, Steam might just return our own profile as a response to the request, for absolutely no reason whatsoever - just try again in this case
		if (!requestOptions.HasFlag(WebBrowser.ERequestOptions.ReturnRedirections) && await IsProfileUri(response.FinalUri).ConfigureAwait(false) && !await IsProfileUri(request).ConfigureAwait(false)) {
			if (--maxTries == 0) {
				return false;
			}

			return await UrlPostWithSession(request, headers, data, referer, requestOptions, session, checkSessionPreemptively, maxTries, rateLimitingDelay, allowSessionRefresh).ConfigureAwait(false);
		}

		return true;
	}

	public static async Task<T> WebLimitRequest<T>(Uri service, Func<Task<T>> function)
	{
		// Sending a request opens a new connection
		await OpenConnectionsSemaphore.WaitAsync().ConfigureAwait(false);

		try
		{
			// It also increases number of requests
			await RateLimitingSemaphore.WaitAsync().ConfigureAwait(false);

			// We release rate-limiter semaphore regardless of our task completion, since we use that one only to guarantee rate-limiting of their creation
			Helper.InBackground(
				async () =>
				{
					await Task.Delay(300).ConfigureAwait(false);
					RateLimitingSemaphore.Release();
				}
			);

			return await function().ConfigureAwait(false);
		}
		finally
		{
			// We release open connections semaphore only once we're indeed done sending a particular request
			OpenConnectionsSemaphore.Release();
		}
	}

	internal async Task<(bool Success, bool RequiresMobileConfirmation)> AcceptTradeOffer(ulong tradeID) {
		if (tradeID == 0) {
			throw new ArgumentOutOfRangeException(nameof(tradeID));
		}

		Uri request = new(SteamCommunityURL, $"/tradeoffer/{tradeID}/accept");
		Uri referer = new(SteamCommunityURL, $"/tradeoffer/{tradeID}");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(3, StringComparer.Ordinal) {
			{ "serverid", "1" },
			{ "tradeofferid", tradeID.ToString(CultureInfo.InvariantCulture) }
		};

		ObjectResponse<TradeOfferAcceptResponse>? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			response = await UrlPostToJsonObjectWithSession<TradeOfferAcceptResponse>(request, data: data, referer: referer, requestOptions: WebBrowser.ERequestOptions.ReturnServerErrors | WebBrowser.ERequestOptions.AllowInvalidBodyOnErrors).ConfigureAwait(false);

			if (response == null) {
				return (false, false);
			}

			if (Core.Utilities.IsServerErrorCode(response.StatusCode)) {
				if (string.IsNullOrEmpty(response.Content?.ErrorText)) {
					// This is a generic server error without a reason, try again
					response = null;

					continue;
				}
				return (false, false);
			}
		}

		return response?.Content != null ? (true, response.Content.RequiresMobileConfirmation) : (false, false);
	}

	internal async Task<bool> ClearFromDiscoveryQueue(uint appID) {
		if (appID == 0) {
			throw new ArgumentOutOfRangeException(nameof(appID));
		}

		Uri request = new(SteamStoreURL, $"/app/{appID}");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(2, StringComparer.Ordinal) { { "appid_to_clear_from_queue", appID.ToString(CultureInfo.InvariantCulture) } };

		return await UrlPostWithSession(request, data: data).ConfigureAwait(false);
	}

	internal async Task<bool> DeclineTradeOffer(ulong tradeID) {
		if (tradeID == 0) {
			throw new ArgumentOutOfRangeException(nameof(tradeID));
		}

		Uri request = new(SteamCommunityURL, $"/tradeoffer/{tradeID}/decline");

		return await UrlPostWithSession(request).ConfigureAwait(false);
	}

	internal HttpClient GenerateDisposableHttpClient() => WebBrowser.GenerateDisposableHttpClient();
	
	internal async Task<HashSet<uint>?> GetAppList() {
		KeyValue? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (WebLimiterDelay > 0)) {
				await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
			}

			using WebAPI.AsyncInterface steamAppsService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(SteamAppsService);

			steamAppsService.Timeout = WebBrowser.Timeout;

			try {
				response = await WebLimitRequest(
					WebAPI.DefaultBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await steamAppsService.CallAsync(HttpMethod.Get, "GetAppList", 2).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				Msg.ShowError("" + e);
			} catch (Exception e) {
				Msg.ShowError("" + e);
			}
		}

		if (response == null) {
			return null;
		}

		List<KeyValue> apps = response["apps"].Children;

		if (apps.Count == 0) {
			return null;
		}

		HashSet<uint> result = new(apps.Count);

		foreach (uint appID in apps.Select(static app => app["appid"].AsUnsignedInteger())) {
			if (appID == 0) {
				return null;
			}

			result.Add(appID);
		}

		return result;
	}

	internal async Task<IDocument?> GetBadgePage(byte page) {
		if (page == 0) {
			throw new ArgumentOutOfRangeException(nameof(page));
		}

		Uri request = new(SteamCommunityURL, $"/my/badges?l=english&p={page}");

		HtmlDocumentResponse? response = await UrlGetToHtmlDocumentWithSession(request, checkSessionPreemptively: false).ConfigureAwait(false);

		return response?.Content;
	}

	internal async Task<byte?> GetCombinedTradeHoldDurationAgainstUser(ulong steamID, string? tradeToken = null) {
		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		(bool success, string? steamApiKey) = await CachedApiKey.GetValue().ConfigureAwait(false);

		if (!success || string.IsNullOrEmpty(steamApiKey)) {
			return null;
		}

		Dictionary<string, object?> arguments = new(!string.IsNullOrEmpty(tradeToken) ? 3 : 2, StringComparer.Ordinal) {
			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			{ "key", steamApiKey! },

			// ReSharper disable once HeapView.BoxingAllocation
			{ "steamid_target", steamID }
		};

		if (!string.IsNullOrEmpty(tradeToken)) {
			// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
			arguments["trade_offer_access_token"] = tradeToken!;
		}

		KeyValue? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (WebLimiterDelay > 0)) {
				await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
			}

			using WebAPI.AsyncInterface econService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(EconService);

			econService.Timeout = WebBrowser.Timeout;

			try {
				response = await WebLimitRequest(
					WebAPI.DefaultBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await econService.CallAsync(HttpMethod.Get, "GetTradeHoldDurations", args: arguments).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				Msg.ShowError("" + e);
			} catch (Exception e) {
				Msg.ShowError("" + e);
			}
		}

		if (response == null) {
			return null;
		}

		uint resultInSeconds = response["both_escrow"]["escrow_end_duration_seconds"].AsUnsignedInteger(uint.MaxValue);

		if (resultInSeconds == uint.MaxValue) {
			return null;
		}

		return resultInSeconds == 0 ? (byte) 0 : (byte) (resultInSeconds / 86400);
	}

	internal async Task<IDocument?> GetConfirmationsPage(string deviceID, string confirmationHash, ulong time) {
		if (string.IsNullOrEmpty(deviceID)) {
			throw new ArgumentNullException(nameof(deviceID));
		}

		if (string.IsNullOrEmpty(confirmationHash)) {
			throw new ArgumentNullException(nameof(confirmationHash));
		}

		if (time == 0) {
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}
		
		if (DateTime.UtcNow - SessionValidUntil > TimeSpan.FromMinutes(5)) {
			if (!await RefreshSession().ConfigureAwait(false)) {
				return null;
			}
		}

		Uri request = new(SteamCommunityURL, $"/mobileconf/conf?a={Bot.SteamID}&k={Uri.EscapeDataString(confirmationHash)}&l=english&m=android&p={Uri.EscapeDataString(deviceID)}&t={time}&tag=conf");

		HtmlDocumentResponse? response = await UrlGetToHtmlDocumentWithSession(request, checkSessionPreemptively: false).ConfigureAwait(false);

		return response?.Content;
	}

	internal async Task<IDocument?> GetDiscoveryQueuePage() {
		Uri request = new(SteamStoreURL, "/explore?l=english");

		HtmlDocumentResponse? response = await UrlGetToHtmlDocumentWithSession(request, checkSessionPreemptively: false).ConfigureAwait(false);

		return response?.Content;
	}

	public static async Task<ulong> GetServerTime() {
		KeyValue? response = null;

		for (byte i = 0; (i < WebBrowser.MaxTries) && (response == null); i++) {
			if ((i > 0) && (WebLimiterDelay > 0)) {
				await Task.Delay(WebLimiterDelay).ConfigureAwait(false);
			}

			using WebAPI.AsyncInterface twoFactorService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(TwoFactorService);

			twoFactorService.Timeout = WebBrowser.Timeout;

			try {
				response = await WebLimitRequest(
					WebAPI.DefaultBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await twoFactorService.CallAsync(HttpMethod.Post, "QueryTime").ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {
				Msg.ShowError("" + e);
			} catch (Exception e) {
				Msg.ShowError("" + e);
			}
		}

		if (response == null) {
			return 0;
		}

		ulong result = response["server_time"].AsUnsignedLong();

		if (result == 0) {
			return 0;
		}

		return result;
	}

	internal async Task<bool?> HandleConfirmation(string deviceID, string confirmationHash, ulong time, ulong confirmationID, ulong confirmationKey, bool accept) {
		if (string.IsNullOrEmpty(deviceID)) {
			throw new ArgumentNullException(nameof(deviceID));
		}

		if (string.IsNullOrEmpty(confirmationHash)) {
			throw new ArgumentNullException(nameof(confirmationHash));
		}

		if (time == 0) {
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		if (confirmationID == 0) {
			throw new ArgumentOutOfRangeException(nameof(confirmationID));
		}

		if (confirmationKey == 0) {
			throw new ArgumentOutOfRangeException(nameof(confirmationKey));
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri request = new(SteamCommunityURL, $"/mobileconf/ajaxop?a={Bot.SteamID}&cid={confirmationID}&ck={confirmationKey}&k={Uri.EscapeDataString(confirmationHash)}&l=english&m=android&op={(accept ? "allow" : "cancel")}&p={Uri.EscapeDataString(deviceID)}&t={time}&tag=conf");

		ObjectResponse<BooleanResponse>? response = await UrlGetToJsonObjectWithSession<BooleanResponse>(request).ConfigureAwait(false);

		return response?.Content?.Success;
	}

	internal async Task<bool?> HandleConfirmations(string deviceID, string confirmationHash, ulong time, IReadOnlyCollection<Confirmation> confirmations, bool accept) {
		if (string.IsNullOrEmpty(deviceID)) {
			throw new ArgumentNullException(nameof(deviceID));
		}

		if (string.IsNullOrEmpty(confirmationHash)) {
			throw new ArgumentNullException(nameof(confirmationHash));
		}

		if (time == 0) {
			throw new ArgumentOutOfRangeException(nameof(time));
		}

		if ((confirmations == null) || (confirmations.Count == 0)) {
			throw new ArgumentNullException(nameof(confirmations));
		}

		if (!Initialized) {
			byte connectionTimeout = GlobalConfig.DefaultConnectionTimeout;

			for (byte i = 0; (i < connectionTimeout) && !Initialized && Bot.IsConnectedAndLoggedOn; i++) {
				await Task.Delay(1000).ConfigureAwait(false);
			}

			if (!Initialized) {
				return null;
			}
		}

		Uri request = new(SteamCommunityURL, "/mobileconf/multiajaxop");

		// Extra entry for sessionID
		List<KeyValuePair<string, string>> data = new(8 + (confirmations.Count * 2)) {
			new KeyValuePair<string, string>("a", Bot.SteamID.ToString(CultureInfo.InvariantCulture)),
			new KeyValuePair<string, string>("k", confirmationHash),
			new KeyValuePair<string, string>("m", "android"),
			new KeyValuePair<string, string>("op", accept ? "allow" : "cancel"),
			new KeyValuePair<string, string>("p", deviceID),
			new KeyValuePair<string, string>("t", time.ToString(CultureInfo.InvariantCulture)),
			new KeyValuePair<string, string>("tag", "conf")
		};

		foreach (Confirmation confirmation in confirmations) {
			data.Add(new KeyValuePair<string, string>("cid[]", confirmation.ID.ToString(CultureInfo.InvariantCulture)));
			data.Add(new KeyValuePair<string, string>("ck[]", confirmation.Key.ToString(CultureInfo.InvariantCulture)));
		}

		ObjectResponse<BooleanResponse>? response = await UrlPostToJsonObjectWithSession<BooleanResponse>(request, data: data).ConfigureAwait(false);

		return response?.Content?.Success;
	}

	internal async Task<bool> Init(ulong steamID, EUniverse universe, string webAPIUserNonce, string? parentalCode = null) {
		if ((steamID == 0) || !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		if ((universe == EUniverse.Invalid) || !Enum.IsDefined(universe)) {
			throw new InvalidEnumArgumentException(nameof(universe), (int) universe, typeof(EUniverse));
		}

		if (string.IsNullOrEmpty(webAPIUserNonce)) {
			throw new ArgumentNullException(nameof(webAPIUserNonce));
		}

		byte[]? publicKey = KeyDictionary.GetPublicKey(universe);

		if ((publicKey == null) || (publicKey.Length == 0)) {
			return false;
		}

		// Generate a random 32-byte session key
		byte[] sessionKey = CryptoHelper.GenerateRandomBlock(32);

		// RSA encrypt our session key with the public key for the universe we're on
		byte[] encryptedSessionKey;

		using (RSACrypto rsa = new(publicKey)) {
			encryptedSessionKey = rsa.Encrypt(sessionKey);
		}

		// Generate login key from the user nonce that we've received from Steam network
		byte[] loginKey = Encoding.UTF8.GetBytes(webAPIUserNonce);

		// AES encrypt our login key with our session key
		byte[] encryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

		Dictionary<string, object?> arguments = new(3, StringComparer.Ordinal) {
			{ "encrypted_loginkey", encryptedLoginKey },
			{ "sessionkey", encryptedSessionKey },
			// ReSharper disable once HeapView.BoxingAllocation
			{ "steamid", steamID }
		};

		KeyValue? response;

		using (WebAPI.AsyncInterface steamUserAuthService = Bot.SteamConfiguration.GetAsyncWebAPIInterface(SteamUserAuthService)) {
			steamUserAuthService.Timeout = WebBrowser.Timeout;

			try {
				response = await WebLimitRequest(
					WebAPI.DefaultBaseAddress,

					// ReSharper disable once AccessToDisposedClosure
					async () => await steamUserAuthService.CallAsync(HttpMethod.Post, "AuthenticateUser", args: arguments).ConfigureAwait(false)
				).ConfigureAwait(false);
			} catch (TaskCanceledException e) {

				return false;
			} catch (Exception e) {

				return false;
			}
		}

		string? steamLogin = response["token"].AsString();

		if (string.IsNullOrEmpty(steamLogin)) {
	
			return false;
		}

		string? steamLoginSecure = response["tokensecure"].AsString();

		if (string.IsNullOrEmpty(steamLoginSecure)) {

			return false;
		}

		string sessionID = Convert.ToBase64String(Encoding.UTF8.GetBytes(steamID.ToString(CultureInfo.InvariantCulture)));

		WebBrowser.CookieContainer.Add(new Cookie("sessionid", sessionID, "/", $".{SteamCommunityURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("sessionid", sessionID, "/", $".{SteamHelpURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("sessionid", sessionID, "/", $".{SteamStoreURL.Host}"));

		WebBrowser.CookieContainer.Add(new Cookie("steamLogin", steamLogin, "/", $".{SteamCommunityURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("steamLogin", steamLogin, "/", $".{SteamHelpURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("steamLogin", steamLogin, "/", $".{SteamStoreURL.Host}"));

		WebBrowser.CookieContainer.Add(new Cookie("steamLoginSecure", steamLoginSecure, "/", $".{SteamCommunityURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("steamLoginSecure", steamLoginSecure, "/", $".{SteamHelpURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("steamLoginSecure", steamLoginSecure, "/", $".{SteamStoreURL.Host}"));

		// Report proper time when doing timezone-based calculations, see setTimezoneCookies() from https://steamcommunity-a.akamaihd.net/public/shared/javascript/shared_global.js
		string timeZoneOffset = $"{(int) DateTimeOffset.Now.Offset.TotalSeconds}{Uri.EscapeDataString(",")}0";

		WebBrowser.CookieContainer.Add(new Cookie("timezoneOffset", timeZoneOffset, "/", $".{SteamCommunityURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("timezoneOffset", timeZoneOffset, "/", $".{SteamHelpURL.Host}"));
		WebBrowser.CookieContainer.Add(new Cookie("timezoneOffset", timeZoneOffset, "/", $".{SteamStoreURL.Host}"));
		
		LastSessionCheck = DateTime.UtcNow;
		SessionValidUntil = LastSessionCheck.AddSeconds(MinimumSessionValidityInSeconds);
		Initialized = true;

		return true;
	}

	internal async Task<bool> MarkSentTrades() {
		Uri request = new(SteamCommunityURL, "/my/tradeoffers/sent");

		return await UrlHeadWithSession(request, checkSessionPreemptively: false).ConfigureAwait(false);
	}

	internal void OnDisconnected() {
		Initialized = false;
		Utilities.InBackground(CachedApiKey.Reset);
	}

	internal void OnVanityURLChanged(string? vanityURL = null) => VanityURL = !string.IsNullOrEmpty(vanityURL) ? vanityURL : null;

	internal async Task<(EResult Result, EPurchaseResultDetail? PurchaseResult, string? BalanceText)?> RedeemWalletKey(string key) {
		if (string.IsNullOrEmpty(key)) {
			throw new ArgumentNullException(nameof(key));
		}

		Uri request = new(SteamStoreURL, "/account/ajaxredeemwalletcode");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(2, StringComparer.Ordinal) { { "wallet_code", key } };

		ObjectResponse<RedeemWalletResponse>? response = await UrlPostToJsonObjectWithSession<RedeemWalletResponse>(request, data: data).ConfigureAwait(false);

		if (response?.Content == null) {
			return null;
		}

		// We can not trust EResult response, because it is OK even in the case of error, so changing it to Fail in this case
		if ((response.Content.Result != EResult.OK) || (response.Content.PurchaseResultDetail != EPurchaseResultDetail.NoDetail)) {
			return (response.Content.Result == EResult.OK ? EResult.Fail : response.Content.Result, response.Content.PurchaseResultDetail, response.Content.BalanceText);
		}

		return (EResult.OK, EPurchaseResultDetail.NoDetail, response.Content.BalanceText);
	}

	internal async Task<bool> UnpackBooster(uint appID, ulong itemID) {
		if (appID == 0) {
			throw new ArgumentOutOfRangeException(nameof(appID));
		}

		if (itemID == 0) {
			throw new ArgumentOutOfRangeException(nameof(itemID));
		}

		string? profileURL = await GetAbsoluteProfileURL().ConfigureAwait(false);

		if (string.IsNullOrEmpty(profileURL)) {
			return false;
		}

		Uri request = new(SteamCommunityURL, $"{profileURL}/ajaxunpackbooster");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(3, StringComparer.Ordinal) {
			{ "appid", appID.ToString(CultureInfo.InvariantCulture) },
			{ "communityitemid", itemID.ToString(CultureInfo.InvariantCulture) }
		};

		ObjectResponse<ResultResponse>? response = await UrlPostToJsonObjectWithSession<ResultResponse>(request, data: data).ConfigureAwait(false);

		return response?.Content?.Result == EResult.OK;
	}
	
	public static INode? SelectSingleNode(IDocument document, string xpath) {
		ArgumentNullException.ThrowIfNull(document);

		return SelectSingleNode(document,xpath);
	}

	public static T? SelectSingleNode<T>(IDocument document, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(document);

		return SelectSingleNode<T>(document.Body,xpath);
	}

	public static T? SelectSingleNode<T>(IElement element, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(element);

		return SelectSingleNode<T>(element,xpath);
	}

	private async Task<(ESteamApiKeyState State, string? Key)> GetApiKeyState() {
		Uri request = new(SteamCommunityURL, "/dev/apikey?l=english");

		using HtmlDocumentResponse? response = await UrlGetToHtmlDocumentWithSession(request, checkSessionPreemptively: false).ConfigureAwait(false);

		if (response?.Content == null) {
			return (ESteamApiKeyState.Timeout, null);
		}

		INode? titleNode = SelectSingleNode(response.Content,"//div[@id='mainContents']/h2");

		if (titleNode == null) {

			return (ESteamApiKeyState.Error, null);
		}

		string title = titleNode.TextContent;

		if (string.IsNullOrEmpty(title)) {

			return (ESteamApiKeyState.Error, null);
		}

		if (title.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) || title.Contains("Validated email address required", StringComparison.OrdinalIgnoreCase)) {
			return (ESteamApiKeyState.AccessDenied, null);
		}
		
		INode? htmlNode = SelectSingleNode(response.Content,"//div[@id='bodyContents_ex']/p");
		
		if (htmlNode == null) {

			return (ESteamApiKeyState.Error, null);
		}

		string text = htmlNode.TextContent;

		if (string.IsNullOrEmpty(text)) {

			return (ESteamApiKeyState.Error, null);
		}

		if (text.Contains("Registering for a Steam Web API Key", StringComparison.OrdinalIgnoreCase)) {
			return (ESteamApiKeyState.NotRegisteredYet, null);
		}

		int keyIndex = text.IndexOf("Key: ", StringComparison.Ordinal);

		if (keyIndex < 0) {

			return (ESteamApiKeyState.Error, null);
		}

		keyIndex += 5;

		if (text.Length <= keyIndex) {

			return (ESteamApiKeyState.Error, null);
		}

		text = text[keyIndex..];

		if ((text.Length != 32) || !Core.Utilities.IsValidHexadecimalText(text)) {

			return (ESteamApiKeyState.Error, null);
		}

		return (ESteamApiKeyState.Registered, text);
	}

	private async Task<bool> IsProfileUri(Uri uri, bool waitForInitialization = true) {
		ArgumentNullException.ThrowIfNull(uri);

		string? profileURL = await GetAbsoluteProfileURL(waitForInitialization).ConfigureAwait(false);

		if (string.IsNullOrEmpty(profileURL)) {

			return false;
		}

		return uri.AbsolutePath.Equals(profileURL, StringComparison.OrdinalIgnoreCase);
	}

	private async Task<bool?> IsSessionExpired() {
		DateTime triggeredAt = DateTime.UtcNow;

		if (triggeredAt <= SessionValidUntil) {
			// Assume session is still valid
			return false;
		}

		await SessionSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if (triggeredAt <= SessionValidUntil) {
				// Other request already checked the session for us in the meantime, nice
				return false;
			}

			if (triggeredAt <= LastSessionCheck) {
				// Other request already checked the session for us in the meantime and failed, pointless to try again
				return true;
			}
			
			Uri request = new(SteamStoreURL, "/account");

			BasicResponse? response = await WebLimitRequest(SteamStoreURL, async () => await WebBrowser.UrlHead(request, rateLimitingDelay: WebLimiterDelay).ConfigureAwait(false)).ConfigureAwait(false);

			if (response == null) {
				return null;
			}

			bool result = IsSessionExpiredUri(response.FinalUri);

			DateTime now = DateTime.UtcNow;

			if (result) {
				Initialized = false;
				SessionValidUntil = DateTime.MinValue;
			} else {
				SessionValidUntil = now.AddSeconds(MinimumSessionValidityInSeconds);
			}

			LastSessionCheck = now;

			return result;
		} finally {
			SessionSemaphore.Release();
		}
	}

	private static bool IsSessionExpiredUri(Uri uri) {
		ArgumentNullException.ThrowIfNull(uri);

		return uri.AbsolutePath.StartsWith("/login", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("lostauth", StringComparison.OrdinalIgnoreCase);
	}

	private static bool ParseItems(IReadOnlyDictionary<(uint AppID, ulong ClassID, ulong InstanceID), InventoryResponseSteam.Description> descriptions, IReadOnlyCollection<KeyValue> input, ICollection<AssetSteam> output) {
		ArgumentNullException.ThrowIfNull(descriptions);

		if ((input == null) || (input.Count == 0)) {
			throw new ArgumentNullException(nameof(input));
		}

		ArgumentNullException.ThrowIfNull(output);

		foreach (KeyValue item in input) {
			uint appID = item["appid"].AsUnsignedInteger();

			if (appID == 0) {

				return false;
			}

			ulong contextID = item["contextid"].AsUnsignedLong();

			if (contextID == 0) {

				return false;
			}

			ulong classID = item["classid"].AsUnsignedLong();

			if (classID == 0) {

				return false;
			}

			ulong instanceID = item["instanceid"].AsUnsignedLong();

			(uint AppID, ulong ClassID, ulong InstanceID) key = (appID, classID, instanceID);

			uint amount = item["amount"].AsUnsignedInteger();

			if (amount == 0) {
				return false;
			}

			ulong assetID = item["assetid"].AsUnsignedLong();

			bool marketable = true;
			bool tradable = true;
			ImmutableHashSet<Tag>? tags = null;
			uint realAppID = 0;
			AssetSteam.EType type = AssetSteam.EType.Unknown;
			AssetSteam.ERarity rarity = AssetSteam.ERarity.Unknown;

			if (descriptions.TryGetValue(key, out InventoryResponseSteam.Description? description)) {
				marketable = description.Marketable;
				tradable = description.Tradable;
				tags = description.Tags;
				realAppID = description.RealAppID;
				type = description.Type;
				rarity = description.Rarity;
			}

			AssetSteam steamAssetSteam = new(appID, contextID, classID, amount, instanceID, assetID, marketable, tradable, tags, realAppID, type, rarity);
			output.Add(steamAssetSteam);
		}

		return true;
	}

	private async Task<bool> RefreshSession() {
		if (!Bot.IsConnectedAndLoggedOn) {
			return false;
		}

		DateTime previousSessionValidUntil = SessionValidUntil;

		DateTime triggeredAt = DateTime.UtcNow;

		await SessionSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if ((triggeredAt <= SessionValidUntil) && (SessionValidUntil > previousSessionValidUntil)) {
				// Other request already refreshed the session for us in the meantime, nice
				return true;
			}

			if (triggeredAt <= LastSessionCheck) {
				// Other request already checked the session for us in the meantime and failed, pointless to try again
				return false;
			}

			Initialized = false;
			SessionValidUntil = DateTime.MinValue;

			if (!Bot.IsConnectedAndLoggedOn) {
				return false;
			}
			
			bool result = await Bot.RefreshSession().ConfigureAwait(false);

			DateTime now = DateTime.UtcNow;

			if (result) {
				SessionValidUntil = now.AddSeconds(MinimumSessionValidityInSeconds);
			}

			LastSessionCheck = now;

			return result;
		} finally {
			SessionSemaphore.Release();
		}
	}

	private async Task<bool> RegisterApiKey() {
		Uri request = new(SteamCommunityURL, "/dev/registerkey");

		// Extra entry for sessionID
		Dictionary<string, string> data = new(4, StringComparer.Ordinal) {
			{ "agreeToTerms", "agreed" },
#pragma warning disable CA1308 // False positive, we're intentionally converting this part to lowercase and it's not used for any security decisions based on the result of the normalization
			{ "domain", $"generated.by.{SharedInfo.AssemblyName.ToLowerInvariant()}.localhost" },
#pragma warning restore CA1308 // False positive, we're intentionally converting this part to lowercase and it's not used for any security decisions based on the result of the normalization
			{ "Submit", "Register" }
		};

		return await UrlPostWithSession(request, data: data).ConfigureAwait(false);
	}

	private async Task<(bool Success, string? Result)> ResolveAccessToken() {
		Uri request = new(SteamStoreURL, "/pointssummary/ajaxgetasyncconfig");

		ObjectResponse<AccessTokenResponse>? response = await UrlGetToJsonObjectWithSession<AccessTokenResponse>(request).ConfigureAwait(false);

		// ReSharper disable once RedundantSuppressNullableWarningExpression - required for .NET Framework
		return !string.IsNullOrEmpty(response?.Content?.Data.WebAPIToken) ? (true, response!.Content!.Data.WebAPIToken) : (false, null);
	}

	private async Task<(bool Success, string? Result)> ResolveApiKey() {
		if (Bot.IsAccountLimited) {
			// API key is permanently unavailable for limited accounts
			return (true, null);
		}

		(ESteamApiKeyState State, string? Key) result = await GetApiKeyState().ConfigureAwait(false);

		switch (result.State) {
			case ESteamApiKeyState.AccessDenied:
				// We succeeded in fetching API key, but it resulted in access denied
				// Return empty result, API key is unavailable permanently
				return (true, "");
			case ESteamApiKeyState.NotRegisteredYet:
				// We succeeded in fetching API key, and it resulted in no key registered yet
				// Let's try to register a new key
				if (!await RegisterApiKey().ConfigureAwait(false)) {
					// Request timed out, bad luck, we'll try again later
					goto case ESteamApiKeyState.Timeout;
				}

				// We should have the key ready, so let's fetch it again
				result = await GetApiKeyState().ConfigureAwait(false);

				if (result.State == ESteamApiKeyState.Timeout) {
					// Request timed out, bad luck, we'll try again later
					goto case ESteamApiKeyState.Timeout;
				}

				if (result.State != ESteamApiKeyState.Registered) {
					// Something went wrong, report error
					goto default;
				}

				goto case ESteamApiKeyState.Registered;
			case ESteamApiKeyState.Registered:
				// We succeeded in fetching API key, and it resulted in registered key
				// Cache the result, this is the API key we want
				return (true, result.Key);
			case ESteamApiKeyState.Timeout:
				// Request timed out, bad luck, we'll try again later
				return (false, null);
			default:
				// We got an unhandled error, this should never happen

				return (false, null);
		}
	}


	public enum ESession : byte {
		None,
		Lowercase,
		CamelCase,
		PascalCase
	}

	private enum ESteamApiKeyState : byte {
		Error,
		Timeout,
		Registered,
		NotRegisteredYet,
		AccessDenied
	}
}