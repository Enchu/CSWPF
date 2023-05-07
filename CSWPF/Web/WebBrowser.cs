using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSWPF.Web.Core;
using CSWPF.Web.Responses;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CSWPF.Web;

public sealed class WebBrowser : IDisposable
{
	public const byte MaxTries = 5;

    internal const byte MaxConnections = 5;

    private const ushort ExtendedTimeout = 600;
    private const byte MaxIdleTime = 15;
    public static TimeSpan Timeout => HttpClient.Timeout;
    
    private static HttpClient HttpClient;
    private static HttpClientHandler HttpClientHandler;
    public CookieContainer CookieContainer { get; } = new();
    
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
    internal WebBrowser(IWebProxy? webProxy = null, bool extendedTimeout = false) {
	    HttpClientHandler = new HttpClientHandler {
		    AllowAutoRedirect = false,

		    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,

		    CookieContainer = CookieContainer
	    };

	    if (webProxy != null) {
		    HttpClientHandler.Proxy = webProxy;
		    HttpClientHandler.UseProxy = true;
	    }
	    
	    HttpClient = GenerateDisposableHttpClient(extendedTimeout);
    }
    
    public void Dispose() {
		HttpClient.Dispose();
		HttpClientHandler.Dispose();
	}

	public static HttpClient GenerateDisposableHttpClient(bool extendedTimeout = false) {
		byte connectionTimeout = ASF.GlobalConfig?.ConnectionTimeout ?? GlobalConfig.DefaultConnectionTimeout;

		HttpClient result = new(HttpClientHandler, false) { 
			DefaultRequestVersion = HttpVersion.Version30,
			Timeout = TimeSpan.FromSeconds(extendedTimeout ? ExtendedTimeout : connectionTimeout)
		};

		result.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(PublicIdentifier, Version.ToString()));
		result.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({BuildInfo.Variant}; {OS.Version.Replace("(", "", StringComparison.Ordinal).Replace(")", "", StringComparison.Ordinal)};)"));
		
		result.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.9));
		result.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.8));

		return result;
	}

	public async Task<BinaryResponse?> UrlGetToBinary(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0, IProgress<byte>? progressReporter = null) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			StreamResponse? response = await UrlGetToStream(request, headers, referer, requestOptions | ERequestOptions.ReturnClientErrors, 1, rateLimitingDelay).ConfigureAwait(false);

			if (response == null) {
				// Request timed out, try again
				continue;
			}

			await using (response.ConfigureAwait(false)) {
				if (response.StatusCode.IsRedirectionCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
						break;
					}
				}

				if (response.StatusCode.IsClientErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
						break;
					}
				}

				if (response.StatusCode.IsServerErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
						continue;
					}
				}

				if (response.Content == null) {
					throw new InvalidOperationException(nameof(response.Content));
				}

				if (response.Length > Array.MaxLength) {
					throw new InvalidOperationException(nameof(response.Length));
				}

				progressReporter?.Report(0);

#pragma warning disable CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose
				MemoryStream ms = new((int) response.Length);
#pragma warning restore CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose

				await using (ms.ConfigureAwait(false)) {
					byte batch = 0;
					long readThisBatch = 0;
					long batchIncreaseSize = response.Length / 100;

					ArrayPool<byte> bytePool = ArrayPool<byte>.Shared;

					// This is HttpClient's buffer, using more doesn't make sense
					byte[] buffer = bytePool.Rent(8192);

					try {
						while (response.Content.CanRead) {
							int read = await response.Content.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

							if (read == 0) {
								break;
							}

							await ms.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);

							if ((progressReporter == null) || (batchIncreaseSize == 0) || (batch >= 99)) {
								continue;
							}

							readThisBatch += read;

							while ((readThisBatch >= batchIncreaseSize) && (batch < 99)) {
								readThisBatch -= batchIncreaseSize;
								progressReporter.Report(++batch);
							}
						}
					} catch (Exception e) {

						return null;
					} finally {
						bytePool.Return(buffer);
					}

					progressReporter?.Report(100);

					return new BinaryResponse(response, ms.ToArray());
				}
			}
		}

		return null;
	}
	
	public async Task<HtmlDocumentResponse?> UrlGetToHtmlDocument(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			StreamResponse? response = await UrlGetToStream(request, headers, referer, requestOptions | ERequestOptions.ReturnClientErrors, 1, rateLimitingDelay).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			await using (response.ConfigureAwait(false)) {
				if (response.StatusCode.IsRedirectionCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
						break;
					}
				}

				if (response.StatusCode.IsClientErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
						break;
					}
				}

				if (response.StatusCode.IsServerErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
						continue;
					}
				}

				if (response.Content == null) {
					throw new InvalidOperationException(nameof(response.Content));
				}

				try {
					return await HtmlDocumentResponse.Create(response).ConfigureAwait(false);
				} catch (Exception e) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new HtmlDocumentResponse(response);
					}
				}
			}
		}

		return null;
	}

	public async Task<ObjectResponse<T>?> UrlGetToJsonObject<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			StreamResponse? response = await UrlGetToStream(request, headers, referer, requestOptions | ERequestOptions.ReturnClientErrors, 1, rateLimitingDelay).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			await using (response.ConfigureAwait(false)) {
				if (response.StatusCode.IsRedirectionCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
						break;
					}
				}

				if (response.StatusCode.IsClientErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
						break;
					}
				}

				if (response.StatusCode.IsServerErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
						continue;
					}
				}

				if (response.Content == null) {
					throw new InvalidOperationException(nameof(response.Content));
				}

				T? obj;

				try {
					using StreamReader streamReader = new(response.Content);

#pragma warning disable CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose
					JsonTextReader jsonReader = new(streamReader);
#pragma warning restore CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose

					await using (jsonReader.ConfigureAwait(false)) {
						JsonSerializer serializer = new();

						obj = serializer.Deserialize<T>(jsonReader);
					}
				} catch (Exception e) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new ObjectResponse<T>(response);
					}

					continue;
				}

				if (obj is null) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new ObjectResponse<T>(response);
					}

					continue;
				}

				return new ObjectResponse<T>(response, obj);
			}
		}

		return null;
	}

	public async Task<StreamResponse?> UrlGetToStream(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			HttpResponseMessage? response = await InternalGet(request, headers, referer, requestOptions, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			if (response.StatusCode.IsRedirectionCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
					break;
				}
			}

			if (response.StatusCode.IsClientErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
					break;
				}
			}

			if (response.StatusCode.IsServerErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
					continue;
				}
			}

			return new StreamResponse(response, await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		return null;
	}
	
	public async Task<BasicResponse?> UrlHead(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			using HttpResponseMessage? response = await InternalHead(request, headers, referer, requestOptions).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			if (response.StatusCode.IsRedirectionCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
					break;
				}
			}

			if (response.StatusCode.IsClientErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
					break;
				}
			}

			if (response.StatusCode.IsServerErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
					continue;
				}
			}

			return new BasicResponse(response);
		}

		return null;
	}
	
	public async Task<BasicResponse?> UrlPost<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, T? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) where T : class {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			using HttpResponseMessage? response = await InternalPost(request, headers, data, referer, requestOptions).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			if (response.StatusCode.IsRedirectionCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
					break;
				}
			}

			if (response.StatusCode.IsClientErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
					break;
				}
			}

			if (response.StatusCode.IsServerErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
					continue;
				}
			}

			return new BasicResponse(response);
		}

		return null;
	}

	public async Task<HtmlDocumentResponse?> UrlPostToHtmlDocument<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, T? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) where T : class {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			StreamResponse? response = await UrlPostToStream(request, headers, data, referer, requestOptions | ERequestOptions.ReturnClientErrors, 1, rateLimitingDelay).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			await using (response.ConfigureAwait(false)) {
				if (response.StatusCode.IsRedirectionCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
						break;
					}
				}

				if (response.StatusCode.IsClientErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
						break;
					}
				}

				if (response.StatusCode.IsServerErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
						continue;
					}
				}

				if (response.Content == null) {
					throw new InvalidOperationException(nameof(response.Content));
				}

				try {
					return await HtmlDocumentResponse.Create(response).ConfigureAwait(false);
				} catch (Exception e) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new HtmlDocumentResponse(response);
					}
				}
			}
		}

		return null;
	}

	public async Task<ObjectResponse<TResult>?> UrlPostToJsonObject<TResult, TData>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, TData? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) where TData : class {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			StreamResponse? response = await UrlPostToStream(request, headers, data, referer, requestOptions | ERequestOptions.ReturnClientErrors, 1, rateLimitingDelay).ConfigureAwait(false);

			if (response == null) {
				continue;
			}

			await using (response.ConfigureAwait(false)) {
				if (response.StatusCode.IsRedirectionCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
						break;
					}
				}

				if (response.StatusCode.IsClientErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
						break;
					}
				}

				if (response.StatusCode.IsServerErrorCode()) {
					if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
						continue;
					}
				}

				if (response.Content == null) {
					throw new InvalidOperationException(nameof(response.Content));
				}

				TResult? obj;

				try {
					using StreamReader streamReader = new(response.Content);

#pragma warning disable CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose
					JsonReader jsonReader = new JsonTextReader(streamReader);
#pragma warning restore CA2000 // False positive, we're actually wrapping it in the using clause below exactly for that purpose

					await using (jsonReader.ConfigureAwait(false)) {
						JsonSerializer serializer = new();

						obj = serializer.Deserialize<TResult>(jsonReader);
					}
				} catch (Exception e) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new ObjectResponse<TResult>(response);
					}

					continue;
				}

				if (obj is null) {
					if ((requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnSuccess) && response.StatusCode.IsSuccessCode()) || (requestOptions.HasFlag(ERequestOptions.AllowInvalidBodyOnErrors) && !response.StatusCode.IsSuccessCode())) {
						return new ObjectResponse<TResult>(response);
					}

					continue;
				}

				return new ObjectResponse<TResult>(response, obj);
			}
		}

		return null;
	}
	
	public async Task<StreamResponse?> UrlPostToStream<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, T? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, byte maxTries = MaxTries, int rateLimitingDelay = 0) where T : class {
		ArgumentNullException.ThrowIfNull(request);

		if (maxTries == 0) {
			throw new ArgumentOutOfRangeException(nameof(maxTries));
		}

		if (rateLimitingDelay < 0) {
			throw new ArgumentOutOfRangeException(nameof(rateLimitingDelay));
		}

		for (byte i = 0; i < maxTries; i++) {
			if ((i > 0) && (rateLimitingDelay > 0)) {
				await Task.Delay(rateLimitingDelay).ConfigureAwait(false);
			}

			HttpResponseMessage? response = await InternalPost(request, headers, data, referer, requestOptions, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

			if (response == null) {
				// Request timed out, try again
				continue;
			}

			if (response.StatusCode.IsRedirectionCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
					break;
				}
			}

			if (response.StatusCode.IsClientErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnClientErrors)) {
					break;
				}
			}

			if (response.StatusCode.IsServerErrorCode()) {
				if (!requestOptions.HasFlag(ERequestOptions.ReturnServerErrors)) {
					continue;
				}
			}

			return new StreamResponse(response, await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		return null;
	}

	internal static void Init() {
		ServicePointManager.DefaultConnectionLimit = MaxConnections;
		ServicePointManager.MaxServicePointIdleTime = MaxIdleTime * 1000;
		ServicePointManager.Expect100Continue = false;
		ServicePointManager.ReusePort = true;

	}

	private async Task<HttpResponseMessage?> InternalGet(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead) {
		ArgumentNullException.ThrowIfNull(request);

		return await InternalRequest<object>(request, HttpMethod.Get, headers, null, referer, requestOptions, httpCompletionOption).ConfigureAwait(false);
	}

	private async Task<HttpResponseMessage?> InternalHead(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead) {
		ArgumentNullException.ThrowIfNull(request);

		return await InternalRequest<object>(request, HttpMethod.Head, headers, null, referer, requestOptions, httpCompletionOption).ConfigureAwait(false);
	}

	private async Task<HttpResponseMessage?> InternalPost<T>(Uri request, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, T? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead) where T : class {
		ArgumentNullException.ThrowIfNull(request);

		return await InternalRequest(request, HttpMethod.Post, headers, data, referer, requestOptions, httpCompletionOption).ConfigureAwait(false);
	}

	private async Task<HttpResponseMessage?> InternalRequest<T>(Uri request, HttpMethod httpMethod, IReadOnlyCollection<KeyValuePair<string, string>>? headers = null, T? data = null, Uri? referer = null, ERequestOptions requestOptions = ERequestOptions.None, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, byte maxRedirections = MaxTries) where T : class {
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(httpMethod);

		HttpResponseMessage response;

		while (true) {
			using (HttpRequestMessage requestMessage = new(httpMethod, request)) {
				requestMessage.Version = HttpClient.DefaultRequestVersion;


				if (headers != null) {
					foreach ((string header, string value) in headers) {
						requestMessage.Headers.Add(header, value);
					}
				}

				if (data != null) {
					switch (data) {
						case HttpContent content:
							requestMessage.Content = content;

							break;
						case IReadOnlyCollection<KeyValuePair<string, string>> nameValueCollection:
							try {
								requestMessage.Content = new FormUrlEncodedContent(nameValueCollection);
							} catch (UriFormatException) {
								requestMessage.Content = new StringContent(string.Join("&", nameValueCollection.Select(static kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")), null, "application/x-www-form-urlencoded");
							}

							break;
						case string text:
							requestMessage.Content = new StringContent(text);

							break;
						default:
							requestMessage.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

							break;
					}
				}

				if (referer != null) {
					requestMessage.Headers.Referrer = referer;
				}


				try {
					response = await HttpClient.SendAsync(requestMessage, httpCompletionOption).ConfigureAwait(false);
				} catch (Exception e) {
					
					return null;
				} finally {
					if (data is HttpContent) {
						requestMessage.Content = null;
					}
				}
			}

			if (response.IsSuccessStatusCode) {
				return response;
			}
			
			if (response.StatusCode.IsRedirectionCode() && (maxRedirections > 0)) {
				if (requestOptions.HasFlag(ERequestOptions.ReturnRedirections)) {
					return response;
				}

				Uri? redirectUri = response.Headers.Location;

				if (redirectUri == null) {
					return null;
				}

				if (redirectUri.IsAbsoluteUri) {
					switch (redirectUri.Scheme) {
						case "http" or "https":
							break;
						case "steammobile":
							return response;
						default:
							// We have no clue about those, but maybe HttpClient can handle them for us
							break;
					}
				} else {
					redirectUri = new Uri(request, redirectUri);
				}

				switch (response.StatusCode) {
					case HttpStatusCode.MovedPermanently: // Per https://tools.ietf.org/html/rfc7231#section-6.4.2, a 301 redirect may be performed using a GET request
					case HttpStatusCode.Redirect: // Per https://tools.ietf.org/html/rfc7231#section-6.4.3, a 302 redirect may be performed using a GET request
					case HttpStatusCode.SeeOther: // Per https://tools.ietf.org/html/rfc7231#section-6.4.4, a 303 redirect should be performed using a GET request
						if (httpMethod != HttpMethod.Head) {
							httpMethod = HttpMethod.Get;
						}
						
						data = null;

						break;
				}

				response.Dispose();
				
				if (!string.IsNullOrEmpty(request.Fragment) && string.IsNullOrEmpty(redirectUri.Fragment)) {
					redirectUri = new UriBuilder(redirectUri) { Fragment = request.Fragment }.Uri;
				}

				request = redirectUri;
				maxRedirections--;

				continue;
			}

			break;
		}

		if (response.StatusCode.IsClientErrorCode()) {
			return response;
		}

		if (requestOptions.HasFlag(ERequestOptions.ReturnServerErrors) && response.StatusCode.IsServerErrorCode()) {
			return response;
		}

		using (response) {
			return null;
		}
	}

	[Flags]
	public enum ERequestOptions : byte {
		None = 0,
		ReturnClientErrors = 1,
		ReturnServerErrors = 2,
		ReturnRedirections = 4,
		AllowInvalidBodyOnSuccess = 8,
		AllowInvalidBodyOnErrors = 16
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