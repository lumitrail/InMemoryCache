using System.Collections.Concurrent;

namespace MinimalCache
{
    /// <summary>
    /// Data wrapper for cache. Data access is locked only when writing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>It's recommended to use immutable reference type or value type for T.</remarks>
    internal class CacheElement<T>
        where T: notnull
    {
        private T _data;

        public T Data
        {
            get
            {
                WaitForWriting();
                T result = _data;
                Interlocked.Exchange(ref _age, 0);
                return result;
            }
            set
            {
                lock (_dataWritingLock)
                {
                    _isWriting = true;
                    _data = value;
                    _isWriting = false;
                }
                Interlocked.Exchange(ref _age, 0);
                LastSet = DateTime.Now;
            }
        }

        /// <summary>When this value is recently set. Used when determining whether this element is expired.</summary>
        public DateTime LastSet { get; private set; } = DateTime.Now;
        private uint _age = 0;
        /// <summary>Ordinal(not strictly) age of this element.</summary>
        public uint Age => _age;

        /// <summary>For lock-free waiting.</summary>
        private volatile bool _isWriting = false;
        /// <summary>_data is locked when writing.</summary>
        private readonly object _dataWritingLock = new object();


        public CacheElement(T data)
        {
            _data = data;
        }


        /// <summary>
        /// Aging this element.
        /// </summary>
        public void MakeOlder()
        {
            Interlocked.Increment(ref _age);
        }

        /// <summary>
        /// Wait for writing.
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
