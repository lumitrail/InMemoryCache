using System.Collections.Concurrent;
using System.Data.SqlTypes;

namespace InMemoryCache
{
    /// <summary>
    /// thread-safe한 in memory cache<br></br>
    /// 사용법: Exists(key)로 캐시 저장되어 있는지 먼저 보고<br></br>
    /// inMemoryCache[key]로 get, set.<br></br>
    /// thread-safe 하므로 static instance 하나 생성해서 사용하시면 됩니다.<br></br>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <exception cref="ArgumentNullException">키 = null</exception>
    /// <exception cref="KeyNotFoundException">캐시 존재하지 않을 때</exception>
    /// <remarks>
    /// 캐시 존재하지 않을 땐 exception을 던짐.<br></br>
    /// exception은 성능에 해가 크므로 미리 Exists(key)로 확인 후 접근하는 것을 추천.
    /// </remarks>
    public class InMemoryCache<TKey, TValue>
        where TKey : notnull
    {
        public int cacheSize { get; private set; }
        public TimeSpan cacheLifeTime { get; private set; }

        private readonly ConcurrentDictionary<TKey, CacheElement<TValue>> cacheObjects;

        public TimeSpan _DEFAULT_CACHE_LIFETIME => TimeSpan.FromDays(30);


        #region Constructors
        private InMemoryCache()
        {
            this.cacheObjects = new();
            this.cacheLifeTime = this._DEFAULT_CACHE_LIFETIME;
        }

        /// <summary>
        /// 캐시 내 객체 수가 cacheSize 미만의 크기로 유지됩니다.
        /// </summary>
        /// <param name="cacheSize"></param>
        public InMemoryCache(int cacheSize) : this()
        {
            if (cacheSize > 0)
            {
                this.cacheSize = cacheSize;
            }
            else
            {
                throw new ArgumentOutOfRangeException("cacheSize", "cache size must be positive.");
            }
        }


        /// <summary>
        /// 캐시 내 객체 수가 cacheSize 미만의 크기로 유지됩니다.<br></br>
        /// 캐시 내 객체가 생성되거나 수정(업데이트)된 지 cacheLifeTime만큼 지나면 삭제됩니다.
        /// </summary>
        /// <param name="cacheSize"></param>
        /// <param name="cacheLifeTime"></param>
        public InMemoryCache(int cacheSize, TimeSpan cacheLifeTime) : this()
        {
            if (cacheSize > 0 && cacheLifeTime > TimeSpan.Zero)
            {
                this.cacheSize = cacheSize;
                this.cacheLifeTime = cacheLifeTime;
            }
            else if (cacheLifeTime <= TimeSpan.Zero) // cache life time error
            {
                throw new ArgumentOutOfRangeException("cacheLifeTime", "cache life time must be positive.");
            }
            else if (cacheSize <= 0) // cache size error
            {
                throw new ArgumentOutOfRangeException("cacheSize", "cache size must be positive.");
            }
            else // both error
            {
                throw new ArgumentOutOfRangeException("cacheSize, cacheLifeTime", "cache size and cache life time must be positive.");
            }
        }
        #endregion


        /// <summary>
        /// 캐시 내용 비우기
        /// </summary>
        public void PurgeCache()
        {
            this.cacheObjects.Clear();
        }


        /// <summary>
        /// 캐시 크기 변경
        /// </summary>
        /// <param name="size">1 미만이면 변경이 무시됨</param>
        public void SetCacheSize(int size)
        {
            if (size < 1
                || this.cacheSize == size)
            {
                return;
            }
            else
            {
                this.cacheSize = size;
                EvictCache();
            }
        }

        /// <summary>
        /// 캐시 수명 변경
        /// </summary>
        /// <param name="minutes">1 미만이면 수명 사용안함</param>
        public void SetCacheLifeTime(int minutes)
        {
            TimeSpan parsedMinute = TimeSpan.FromMinutes(minutes);

            if (minutes < 1)
            {
                //this.useCacheLifeTime = false;
                this.cacheLifeTime = TimeSpan.MaxValue;
            }
            else
            {
                //this.useCacheLifeTime = true;
                this.cacheLifeTime = parsedMinute;
                EvictCache();
            }
        }


        /// <summary>
        /// 캐시에 key가 존재하는지 확인
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(TKey key)
        {
            try
            {
                if (key != null)
                {
                    return this.cacheObjects.ContainsKey(key)
                        && this.cacheObjects[key].age < this.cacheSize
                        && (this.cacheObjects[key].updatedTime + this.cacheLifeTime) > DateTime.Now;
                }
            }
            catch
            {
            }
            return false;
        }

        public bool TryRead(TKey key, out TValue outValue)
        {
            outValue = default(TValue);

            if (key != null)
            {
                if (Exists(key))
                {
                    try
                    {
                        outValue = this.cacheObjects[key].data;
                        // EvictCache();
                        return true;
                    }
                    catch
                    {
                    }
                }
            }

            return false;
        }

        public bool TryWrite(TKey key, TValue value)
        {
            try
            {
                this[key] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// [key]로 get, set
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="OverflowException"></exception>
        /// <exception cref="AggregateException"></exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue result;
                if (TryRead(key, out result))
                {
                    // EvictCache();
                    return result;
                }
                throw new KeyNotFoundException();
            }
            set
            {
                CacheElement<TValue> newValue = new(value);
                this.cacheObjects[key] = newValue;
                EvictCache();
            }
        }

        private void EvictCache()
        {
            //Parallel.ForEach(cacheObjects.Keys, key =>
            foreach (TKey key in cacheObjects.Keys)
            {
                try
                {
                    CacheElement<TValue> cacheObject;
                    if (this.cacheObjects.TryGetValue(key, out cacheObject))
                    {
                        cacheObject.GetOlder();

                        if (cacheObject.age > this.cacheSize)
                        {
                            this.cacheObjects.TryRemove(key, out cacheObject);
                        }

                        if (cacheObject.updatedTime + this.cacheLifeTime < DateTime.Now)
                        {
                            this.cacheObjects.TryRemove(key, out cacheObject);
                        }
                    }
                }
                catch
                {
                }
            }
            //);

            return;
        }
    }
}
