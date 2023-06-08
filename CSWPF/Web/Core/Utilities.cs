﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.XPath;
using CSWPF.Utils;
using Humanizer;
using Humanizer.Localisation;
using SteamKit2;
using Zxcvbn;

namespace CSWPF.Web.Core;

public static class Utilities {
	private const byte TimeoutForLongRunningTasksInSeconds = 60;

	// normally we'd just use words like "steam" and "farm", but the library we're currently using is a bit iffy about banned words, so we need to also add combinations such as "steamfarm"
	private static readonly ImmutableHashSet<string> ForbiddenPasswordPhrases = ImmutableHashSet.Create(StringComparer.InvariantCultureIgnoreCase, "archisteamfarm", "archi", "steam", "farm", "archisteam", "archifarm", "steamfarm", "asf", "asffarm", "password");

	public static string GetArgsAsText(string[] args, byte argsToSkip, string delimiter) {
		ArgumentNullException.ThrowIfNull(args);

		if (args.Length <= argsToSkip) {
			throw new InvalidOperationException($"{nameof(args.Length)} && {nameof(argsToSkip)}");
		}

		if (string.IsNullOrEmpty(delimiter)) {
			throw new ArgumentNullException(nameof(delimiter));
		}

		return string.Join(delimiter, args.Skip(argsToSkip));
	}

	public static string GetArgsAsText(string text, byte argsToSkip) {
		if (string.IsNullOrEmpty(text)) {
			throw new ArgumentNullException(nameof(text));
		}

		string[] args = text.Split(Array.Empty<char>(), argsToSkip + 1, StringSplitOptions.RemoveEmptyEntries);

		return args[^1];
	}

	public static string? GetCookieValue(this CookieContainer cookieContainer, Uri uri, string name) {
		ArgumentNullException.ThrowIfNull(cookieContainer);
		ArgumentNullException.ThrowIfNull(uri);

		if (string.IsNullOrEmpty(name)) {
			throw new ArgumentNullException(nameof(name));
		}

		CookieCollection cookies = cookieContainer.GetCookies(uri);
		return cookies.Count > 0 ? (from Cookie cookie in cookies where cookie.Name == name select cookie.Value).FirstOrDefault() : null;

	}
	
	public static ulong GetUnixTime() => (ulong) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

	public static async void InBackground(Action action, bool longRunning = false) {
		ArgumentNullException.ThrowIfNull(action);

		TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

		if (longRunning) {
			options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
		}

		await Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default).ConfigureAwait(false);
	}

	public static void InBackground<T>(Func<T> function, bool longRunning = false) {
		ArgumentNullException.ThrowIfNull(function);

		InBackground(void() => function(), longRunning);
	}

	public static bool IsClientErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError;

	public static bool IsRedirectionCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.Ambiguous and < HttpStatusCode.BadRequest;

	public static bool IsServerErrorCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.InternalServerError and < (HttpStatusCode) 600;

	public static bool IsSuccessCode(this HttpStatusCode statusCode) => statusCode is >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous;
	

	public static bool IsValidHexadecimalText(string text) {
		if (string.IsNullOrEmpty(text)) {
			throw new ArgumentNullException(nameof(text));
		}

		return (text.Length % 2 == 0) && text.All(Uri.IsHexDigit);
	}

	public static IList<INode> SelectNodes(this IDocument document, string xpath) {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectNodes(xpath);
	}

	public static IEnumerable<T> SelectNodes<T>(this IDocument document, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectNodes(xpath).OfType<T>();
	}

	public static IEnumerable<T> SelectNodes<T>(this IElement element, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(element);

		return element.SelectNodes(xpath).OfType<T>();
	}

	public static INode? SelectSingleNode(this IDocument document, string xpath) {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectSingleNode(xpath);
	}

	public static T? SelectSingleNode<T>(this IDocument document, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(document);

		return document.Body.SelectSingleNode(xpath) as T;
	}

	public static T? SelectSingleNode<T>(this IElement element, string xpath) where T : class, INode {
		ArgumentNullException.ThrowIfNull(element);

		return element.SelectSingleNode(xpath) as T;
	}

	public static IEnumerable<T> ToEnumerable<T>(this T item) {
		yield return item;
	}

	public static string ToHumanReadable(this TimeSpan timeSpan) => timeSpan.Humanize(3, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second);

	public static Task<T> ToLongRunningTask<T>(this AsyncJob<T> job) where T : CallbackMsg {
		ArgumentNullException.ThrowIfNull(job);

		job.Timeout = TimeSpan.FromSeconds(TimeoutForLongRunningTasksInSeconds);

		return job.ToTask();
	}
	
	public static Task<AsyncJobMultiple<T>.ResultSet> ToLongRunningTask<T>(this AsyncJobMultiple<T> job) where T : CallbackMsg {
		ArgumentNullException.ThrowIfNull(job);

		job.Timeout = TimeSpan.FromSeconds(TimeoutForLongRunningTasksInSeconds);

		return job.ToTask();
	}

	internal static void DeleteEmptyDirectoriesRecursively(string directory) {
		if (string.IsNullOrEmpty(directory)) {
			throw new ArgumentNullException(nameof(directory));
		}

		if (!System.IO.Directory.Exists(directory)) {
			return;
		}

		try {
			foreach (string subDirectory in System.IO.Directory.EnumerateDirectories(directory)) {
				DeleteEmptyDirectoriesRecursively(subDirectory);
			}

			if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) {
				System.IO.Directory.Delete(directory);
			}
		} catch (Exception e) {
			Msg.ShowError(""+e);
		}
	}

	internal static ulong MathAdd(ulong first, int second) {
		if (second >= 0) {
			return first + (uint) second;
		}

		return first - (uint) -second;
	}

	internal static bool RelativeDirectoryStartsWith(string directory, params string[] prefixes) {
		if (string.IsNullOrEmpty(directory)) {
			throw new ArgumentNullException(nameof(directory));
		}

#pragma warning disable CA1508 // False positive, params could be null when explicitly set
		if ((prefixes == null) || (prefixes.Length == 0)) {
#pragma warning restore CA1508 // False positive, params could be null when explicitly set
			throw new ArgumentNullException(nameof(prefixes));
		}

		return (from prefix in prefixes where directory.Length > prefix.Length let pathSeparator = directory[prefix.Length] where (pathSeparator == Path.DirectorySeparatorChar) || (pathSeparator == Path.AltDirectorySeparatorChar) select prefix).Any(prefix => directory.StartsWith(prefix, StringComparison.Ordinal));
	}

	internal static (bool IsWeak, string? Reason) TestPasswordStrength(string password, ISet<string>? additionallyForbiddenPhrases = null) {
		if (string.IsNullOrEmpty(password)) {
			throw new ArgumentNullException(nameof(password));
		}

		HashSet<string> forbiddenPhrases = ForbiddenPasswordPhrases.ToHashSet(StringComparer.InvariantCultureIgnoreCase);

		if (additionallyForbiddenPhrases != null) {
			forbiddenPhrases.UnionWith(additionallyForbiddenPhrases);
		}

		Result result = Zxcvbn.Core.EvaluatePassword(password, forbiddenPhrases);

		IList<string>? suggestions = result.Feedback.Suggestions;

		if (!string.IsNullOrEmpty(result.Feedback.Warning)) {
			suggestions ??= new List<string>(1);

			suggestions.Insert(0, result.Feedback.Warning);
		}

		if (suggestions != null) {
			for (byte i = 0; i < suggestions.Count; i++) {
				string suggestion = suggestions[i];

				if ((suggestion.Length == 0) || (suggestion[^1] == '.')) {
					continue;
				}

				suggestions[i] = $"{suggestion}.";
			}
		}

		return (result.Score < 4, suggestions is { Count: > 0 } ? string.Join(" ", suggestions.Where(static suggestion => suggestion.Length > 0)) : null);
	}

	internal static void WarnAboutIncompleteTranslation(ResourceManager resourceManager) {
		ArgumentNullException.ThrowIfNull(resourceManager);

		// Skip translation progress for English and invariant (such as "C") cultures
		switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) {
			case "en" or "iv" or "qps":
				return;
		}

		// We can't dispose this resource set, as we can't be sure if it isn't used somewhere else, rely on GC in this case
		ResourceSet? defaultResourceSet = resourceManager.GetResourceSet(CultureInfo.GetCultureInfo("en-US"), true, true);

		if (defaultResourceSet == null) {
			Msg.ShowError(""+defaultResourceSet);

			return;
		}

		HashSet<DictionaryEntry> defaultStringObjects = defaultResourceSet.Cast<DictionaryEntry>().ToHashSet();

		if (defaultStringObjects.Count == 0) {
			Msg.ShowError(""+defaultStringObjects);

			return;
		}

		// We can't dispose this resource set, as we can't be sure if it isn't used somewhere else, rely on GC in this case
		ResourceSet? currentResourceSet = resourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);

		if (currentResourceSet == null) {
			Msg.ShowError(""+currentResourceSet);

			return;
		}

		HashSet<DictionaryEntry> currentStringObjects = currentResourceSet.Cast<DictionaryEntry>().ToHashSet();

		if (currentStringObjects.Count >= defaultStringObjects.Count) {
			// Either we have 100% finished translation, or we're missing it entirely and using en-US
			HashSet<DictionaryEntry> testStringObjects = currentStringObjects.ToHashSet();
			testStringObjects.ExceptWith(defaultStringObjects);

			// If we got 0 as final result, this is the missing language
			// Otherwise it's just a small amount of strings that happen to be the same
			if (testStringObjects.Count == 0) {
				currentStringObjects = testStringObjects;
			}
		}
	}
}