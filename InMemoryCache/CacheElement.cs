namespace InMemoryCache
{
    /// <summary>
    /// It's recommended to use immutable a reference type or a primitive type for T.
    /// </summary>
    /// <typeparam name="T">reference type or a primitive type</typeparam>
    internal class CacheElement<T>
    {
        /// <summary>When this value is recently set. Used when determining whether this element is expired.</summary>
        public DateTime LastSet { get; private set; }
        /// <summary>Ordinal(not strictly) age of this element.</summary>
        public uint Age { get; private set; }

        private T _data;
        /// <summary>Getting and Setting changes other member values.</summary>
        public T Data
        {
            get
            {
                WaitForWriting();
                lock (_memberWritingLock)
                {
                    Age = 0;
                }
                return _data;
            }
            set
            {
                // Won't wait for reading
                lock (_memberWritingLock)
                {
                    _isWriting = true;
                    _data = value;
                    _isWriting = false;
                    
                    Age = 0;
                    LastSet = DateTime.Now;
                }
            }
        }

        private volatile bool _isWriting;
        private readonly object _memberWritingLock;


        public CacheElement(T data)
        {
            LastSet = DateTime.Now;
            Age = 0;

            _data = data;

            _isWriting = false;
            _memberWritingLock = new object();
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

        /// <summary>
        /// Lockless wait until _isWriting becomes false.
        /// </summary>
        private void WaitForWriting()
        {
            var spinner = new SpinWait();
            while (_isWriting)
            {
                spinner.SpinOnce();
            }
        }
    }
}
