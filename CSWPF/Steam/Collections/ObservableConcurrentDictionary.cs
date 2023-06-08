using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CSWPF.Steam.Collections;

public sealed class ObservableConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull {
	public event EventHandler? OnModified;

	public int Count => BackingDictionary.Count;

	public bool IsEmpty => BackingDictionary.IsEmpty;

	public bool IsReadOnly => false;

	[JsonProperty(Required = Required.DisallowNull)]
	private readonly ConcurrentDictionary<TKey, TValue> BackingDictionary = new();

	int ICollection<KeyValuePair<TKey, TValue>>.Count => BackingDictionary.Count;
	int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => BackingDictionary.Count;
	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => BackingDictionary.Keys;
	ICollection<TKey> IDictionary<TKey, TValue>.Keys => BackingDictionary.Keys;
	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => BackingDictionary.Values;
	ICollection<TValue> IDictionary<TKey, TValue>.Values => BackingDictionary.Values;

	public TValue this[TKey key] {
		get => BackingDictionary[key];
		set {
			if (BackingDictionary.TryGetValue(key, out TValue? savedValue) && EqualityComparer<TValue>.Default.Equals(savedValue, value)) {
				return;
			}

			BackingDictionary[key] = value;
			OnModified?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Add(KeyValuePair<TKey, TValue> item) {
		(TKey key, TValue value) = item;

		Add(key, value);
	}

	public void Add(TKey key, TValue value) => TryAdd(key, value);

	public void Clear() {
		if (BackingDictionary.IsEmpty) {
			return;
		}

		BackingDictionary.Clear();
		OnModified?.Invoke(this, EventArgs.Empty);
	}

	public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>) BackingDictionary).Contains(item);
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>) BackingDictionary).CopyTo(array, arrayIndex);
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => BackingDictionary.GetEnumerator();

	public bool Remove(KeyValuePair<TKey, TValue> item) {
		ICollection<KeyValuePair<TKey, TValue>> collection = BackingDictionary;

		if (!collection.Remove(item)) {
			return false;
		}

		OnModified?.Invoke(this, EventArgs.Empty);

		return true;
	}

	public bool Remove(TKey key) {
		if (!BackingDictionary.TryRemove(key, out _)) {
			return false;
		}

		OnModified?.Invoke(this, EventArgs.Empty);

		return true;
	}

	bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => BackingDictionary.ContainsKey(key);
	bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => BackingDictionary.ContainsKey(key);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => BackingDictionary.TryGetValue(key, out value!);
	bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => BackingDictionary.TryGetValue(key, out value!);

	public bool TryAdd(TKey key, TValue value) {
		if (!BackingDictionary.TryAdd(key, value)) {
			return false;
		}

		OnModified?.Invoke(this, EventArgs.Empty);

		return true;
	}

	public bool TryGetValue(TKey key, out TValue? value) => BackingDictionary.TryGetValue(key, out value);
}