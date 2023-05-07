using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CSWPF.Utils;
using Newtonsoft.Json;

namespace CSWPF.Web.Helpe;

public abstract class SerializableFile : IDisposable {
	private static readonly SemaphoreSlim GlobalFileSemaphore = new(1, 1);

	private readonly SemaphoreSlim FileSemaphore = new(1, 1);

	protected string? FilePath { get; set; }

	private bool ReadOnly;
	private bool SavingScheduled;

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) {
		if (disposing) {
			FileSemaphore.Dispose();
		}
	}

	protected async Task Save() {
		if (string.IsNullOrEmpty(FilePath)) {
			throw new InvalidOperationException(nameof(FilePath));
		}

		if (ReadOnly) {
			return;
		}
		lock (FileSemaphore) {
			if (SavingScheduled) {
				return;
			}

			SavingScheduled = true;
		}

		await FileSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			lock (FileSemaphore) {
				SavingScheduled = false;
			}

			if (ReadOnly) {
				return;
			}

			string json = JsonConvert.SerializeObject(this);

			if (string.IsNullOrEmpty(json)) {
				throw new InvalidOperationException(nameof(json));
			}
			
			string newFilePath = $"{FilePath}.new";

			if (File.Exists(FilePath)) {
				string currentJson = await File.ReadAllTextAsync(FilePath!).ConfigureAwait(false);

				if (json == currentJson) {
					return;
				}

				await File.WriteAllTextAsync(newFilePath, json).ConfigureAwait(false);

				File.Replace(newFilePath, FilePath!, null);
			} else {
				await File.WriteAllTextAsync(newFilePath, json).ConfigureAwait(false);

				File.Move(newFilePath, FilePath!);
			}
		} catch (Exception e) {
			Msg.ShowError(e.ToString());
		} finally {
			FileSemaphore.Release();
		}
	}

	internal async Task MakeReadOnly() {
		if (ReadOnly) {
			return;
		}

		await FileSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if (ReadOnly) {
				return;
			}

			ReadOnly = true;
		} finally {
			FileSemaphore.Release();
		}
	}

	internal static async Task<bool> Write(string filePath, string json) {
		if (string.IsNullOrEmpty(filePath)) {
			throw new ArgumentNullException(nameof(filePath));
		}

		if (string.IsNullOrEmpty(json)) {
			throw new ArgumentNullException(nameof(json));
		}

		string newFilePath = $"{filePath}.new";

		await GlobalFileSemaphore.WaitAsync().ConfigureAwait(false);

		try {
			if (File.Exists(filePath)) {
				string currentJson = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

				if (json == currentJson) {
					return true;
				}

				await File.WriteAllTextAsync(newFilePath, json).ConfigureAwait(false);

				File.Replace(newFilePath, filePath, null);
			} else {
				await File.WriteAllTextAsync(newFilePath, json).ConfigureAwait(false);

				File.Move(newFilePath, filePath);
			}

			return true;
		} catch (Exception e) {
			Msg.ShowError(e.ToString());
			return false;
		} finally {
			GlobalFileSemaphore.Release();
		}
	}
}