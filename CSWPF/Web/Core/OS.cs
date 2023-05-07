using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CSWPF.Web.Core;

internal static class OS {
	internal static readonly string ProcessFileName = Environment.ProcessPath ?? throw new InvalidOperationException(nameof(ProcessFileName));

	internal static DateTime ProcessStartTime {
		get {
			using Process process = Process.GetCurrentProcess();

			return process.StartTime.ToUniversalTime();
		}

	}

	internal static string Version {
		get {
			if (!string.IsNullOrEmpty(BackingVersion)) {
				return BackingVersion!;
			}

			string framework = RuntimeInformation.FrameworkDescription.Trim();

			if (framework.Length == 0) {
				framework = "Unknown Framework";
			}

#if NETFRAMEWORK
			string runtime = RuntimeInformation.OSArchitecture.ToString();
#else
			string runtime = RuntimeInformation.RuntimeIdentifier.Trim();

			if (runtime.Length == 0) {
				runtime = "Unknown Runtime";
			}
#endif

			string description = RuntimeInformation.OSDescription.Trim();

			if (description.Length == 0) {
				description = "Unknown OS";
			}

			BackingVersion = $"{framework}; {runtime}; {description}";

			return BackingVersion;
		}
	}

	private static string? BackingVersion;
	private static Mutex? SingleInstance;


	internal static void Init(GlobalConfig.EOptimizationMode optimizationMode) {
		if (!Enum.IsDefined(optimizationMode)) {
			throw new InvalidEnumArgumentException(nameof(optimizationMode), (int) optimizationMode, typeof(GlobalConfig.EOptimizationMode));
		}

		switch (optimizationMode) {
			case GlobalConfig.EOptimizationMode.MaxPerformance:
				break;
			case GlobalConfig.EOptimizationMode.MinMemoryUsage:
				Regex.CacheSize = 0;

				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(optimizationMode));
		}
	}
}