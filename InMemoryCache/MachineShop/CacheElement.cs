namespace InMemoryCache.MachineShop
{
    /// <summary>
    /// Data wrapper for cache. Data access is locked only when writing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>It's recommended to use a immutable reference type or a primitive type for T.</remarks>
    internal class CacheElement<T> : Element<T>
    {
        /// <summary>When this value is recently set. Used when determining whether this element is expired.</summary>
        public DateTime LastSet { get; private set; }
        /// <summary>Ordinal(not strictly) age of this element.</summary>
        public uint Age { get; private set; }


        public CacheElement(T data) : base(data)
        {
            LastSet = DateTime.Now;
            Age = 0;
        }


        /// <summary>
        /// Aging this element.
        /// </summary>
        /// <returns></returns>
        public void MakeOlder()
        {
            lock (_memberWritingLock)
            {
                ++Age;
            }
        }

        protected override void BeforeGet()
        {
            base.BeforeGet();
            lock (_memberWritingLock)
            {
                Age = 0;
            }
        }

        protected override void AfterSet()
        {
            base.AfterSet();
            lock (_memberWritingLock)
            {
                Age = 0;
                LastSet = DateTime.Now;
            }
        }
    }
}
