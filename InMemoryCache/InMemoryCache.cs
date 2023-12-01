using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using InMemoryCache.MachineShop;

namespace InMemoryCache
{
    /// <summary>
    /// Thread-safe key-value cache.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class InMemoryCache<TKey, TValue>
        where TKey: notnull
    {
        /// <summary>128</summary>
        public static readonly int DefaultCapacity = 128;
        /// <summary>30 days</summary>
        public static readonly TimeSpan DefaultElementLifeSpan = TimeSpan.FromDays(30);

        /// <summary>Cap of the cache element count.</summary>
        public int Capacity { get; private set; }
        /// <summary>Defines when a cache element is evicted.</summary>
        public TimeSpan ElementLifeSpan { get; private set; }

        /// <summary>
        /// Get and set using index
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out TValue? value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                CacheElement<TValue> newValue = new(value);
                _cacheElements[key] = newValue;
                Evict();
            }
        }

        private readonly ConcurrentDictionary<TKey, CacheElement<TValue>> _cacheElements;


        #region Constructors
        /// <summary>
        /// New InMemoryCache with DefaultCapacity and DefaultElementLifeSpan.
        /// </summary>
        public InMemoryCache()
        {
            Capacity = DefaultCapacity;
            ElementLifeSpan = DefaultElementLifeSpan;

            _cacheElements = new ConcurrentDictionary<TKey, CacheElement<TValue>>();
        }

        /// <summary>
        /// New InMemoryCache with capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public InMemoryCache(int capacity)
            : this()
        {
            if (capacity > 0)
            {
                Capacity = capacity;
            }
        }

        /// <summary>
        /// New InMemoryCache with capacity and elementLifeSpan.
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="elementLifeSpan"></param>
        public InMemoryCache(int capacity, TimeSpan elementLifeSpan)
            : this(capacity)
        {
            if (elementLifeSpan > TimeSpan.Zero)
            {
                ElementLifeSpan = elementLifeSpan;
            }
        }
        #endregion


        /// <summary>
        /// Removes all elements from this cache.
        /// </summary>
        public void Clear()
        {
            _cacheElements.Clear();
        }

        /// <summary>
        /// Gets current count of objects in this cache.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            Evict();
            return _cacheElements.Count;
        }

        /// <summary>
        /// Sets new capacity and evicts elements exceeding the capacity.
        /// </summary>
        /// <param name="newCapacity"></param>
        public void SetCapacity(int newCapacity)
        {
            if (newCapacity > 0)
            {
                Capacity = newCapacity;
            }
            else
            {
                Capacity = 1;
            }

            Evict();
        }

        /// <summary>
        /// Sets new capacity and evicts expired elements.
        /// </summary>
        /// <param name="newLifeSpan"></param>
        public void SetElementLifeSpan(TimeSpan newLifeSpan)
        {
            if (newLifeSpan > TimeSpan.Zero)
            {
                ElementLifeSpan = newLifeSpan;
            }

            Evict();
        }

        /// <summary>
        /// Checks if this cache contains key and its value is not expired or evicted.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsValidKey(TKey key)
        {
            try
            {
                return _cacheElements.ContainsKey(key)
                        && _cacheElements[key].Age < Capacity
                        && (_cacheElements[key].LastSet + ElementLifeSpan) > DateTime.Now;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to read cache with key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>true if get is successful, otherwise false.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (ContainsValidKey(key)
                && _cacheElements.TryGetValue(key, out CacheElement<TValue>? cacheElement))
            {
                AgeElements();
                value = cacheElement.Data;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to write cache with key. Then eviction comes.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TrySetValue(TKey key, TValue value)
        {
            try
            {
                AgeElements();
                _cacheElements[key] = new CacheElement<TValue>(value);
                Evict();
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void AgeElements()
        {
            int cacheElemCount = _cacheElements.Count;
            if (cacheElemCount > 16384)
            {
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = cacheElemCount / 8192
                };
                Parallel.ForEach(_cacheElements.Keys, parallelOptions, Age);
            }
            else
            {
                foreach (TKey key in _cacheElements.Keys)
                {
                    Age(key);
                }
            }
        }

        private void Age(TKey key)
        {
            try
            {
                if (_cacheElements.TryGetValue(key, out CacheElement<TValue>? value))
                {
                    value.MakeOlder();
                }
            }
            catch { }
        }

        /// <summary>
        /// Removes key-value pairs in this cache if the pairs are LRU out of capacity OR expired.
        /// </summary>
        private void Evict()
        {
            int cacheElemCount = _cacheElements.Count;
            if (cacheElemCount > 16384)
            {
                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = cacheElemCount / 8192
                };
                Parallel.ForEach(_cacheElements.Keys, parallelOptions, EvictWhenNeeded);
            }
            else
            {
                foreach (TKey key in _cacheElements.Keys)
                {
                    EvictWhenNeeded(key);
                }
            }
        }

        /// <summary>
        /// Evicts key-value pair when it's LRU out of capacity OR expired.
        /// </summary>
        /// <param name="key"></param>
        private void EvictWhenNeeded(TKey key)
        {
            try
            {
                if (_cacheElements.TryGetValue(key, out CacheElement<TValue>? cacheElement))
                {
                    if (cacheElement.Age > Capacity
                        || cacheElement.LastSet + ElementLifeSpan < DateTime.Now)
                    {
                        _cacheElements.TryRemove(key, out _);
                    }
                }
            }
            catch { }
        }
    }
}
