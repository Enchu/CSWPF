using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CSWPF.Web.Assistance;
using JetBrains.Annotations;

namespace CSWPF.Web;

public sealed class Cacheable<T> : IDisposable {
	private readonly TimeSpan CacheLifetime;
	private readonly SemaphoreSlim InitSemaphore = new(1, 1);
	private readonly Func<Task<(bool Success, T? Result)>> ResolveFunction;

	private bool IsInitialized => InitializedAt > DateTime.MinValue;
	private bool IsPermanentCache => CacheLifetime == Timeout.InfiniteTimeSpan;
	private bool IsRecent => IsPermanentCache || (DateTime.UtcNow.Subtract(InitializedAt) < CacheLifetime);

	private DateTime InitializedAt;
	private T? InitializedValue;

	public Cacheable(Func<Task<(bool Success, T? Result)>> resolveFunction, TimeSpan? cacheLifetime = null) {
		ResolveFunction = resolveFunction ?? throw new ArgumentNullException(nameof(resolveFunction));
		CacheLifetime = cacheLifetime ?? Timeout.InfiniteTimeSpan;
	}

	public void Dispose() => InitSemaphore.Dispose();
	
	public async Task<(bool Success, T? Result)> GetValue(ECacheFallback cacheFallback = ECacheFallback.DefaultForType) {
		if (!Enum.IsDefined(cacheFallback)) {
			throw new InvalidEnumArgumentException(nameof(cacheFallback), (int) cacheFallback, typeof(ECacheFallback));
		}

		if (IsInitialized && IsRecent) {
			return (true, InitializedValue);
		}

		await InitSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if (IsInitialized && IsRecent) {
				return (true, InitializedValue);
			}

			(bool success, T? result) = await ResolveFunction().ConfigureAwait(false);

			if (!success) {
				return cacheFallback switch {
					ECacheFallback.DefaultForType => (false, default(T?)),
					ECacheFallback.FailedNow => (false, result),
					ECacheFallback.SuccessPreviously => (false, InitializedValue),
					_ => throw new InvalidOperationException(nameof(cacheFallback))
				};
			}

			InitializedValue = result;
			InitializedAt = DateTime.UtcNow;

			return (true, result);
		} finally {
			InitSemaphore.Release();
		}
	}
	
	public async Task Reset() {
		if (!IsInitialized) {
			return;
		}

		await InitSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if (!IsInitialized) {
				return;
			}

			InitializedAt = DateTime.MinValue;
			InitializedValue = default(T?);
		} finally {
			InitSemaphore.Release();
		}
	}

	public enum EFallback : byte {
		DefaultForType,
		FailedNow,
		SuccessPreviously
	}
}