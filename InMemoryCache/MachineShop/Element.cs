using System.Collections.Generic;

namespace InMemoryCache.MachineShop
{
    /// <summary>
    /// Data wrapper of which data access is locked only when writing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class Element<T>
    {
        private T _data;
        /// <summary>Getting and Setting changes other member values.</summary>
        public T Data
        {
            get
            {
                BeforeGet();
                T result = _data;
                AfterGet();
                return result;
            }
            set
            {
                // Won't wait for reading
                BeforeSet();
                _data = value;
                AfterSet();
            }
        }

        Semaphore _readingSemaphore;
        Semaphore _writingSemaphore;

        protected volatile int _readingThreads;
        protected volatile bool _isWriting;
        protected readonly object _memberWritingLock;


        public Element(T data)
        {
            _data = data;

            _readingSemaphore = new Semaphore(0, 1024);
            _writingSemaphore = new Semaphore(0, 1);

            _readingThreads = 0;
            _isWriting = false;
            _memberWritingLock = new object();
        }


        /// <summary>
        /// What should be done before Get Data.
        /// </summary>
        protected virtual void BeforeGet()
        {
            WaitForWriting();
            //EnterReading();
        }

        /// <summary>
        /// What should be done after Get Data.
        /// </summary>
        protected virtual void AfterGet()
        {
            //ExitReading();
        }

        /// <summary>
        /// What should be done before Set Data.
        /// </summary>
        protected virtual void BeforeSet()
        {
            WaitForWriting();
            EnterWriting();
            //WaitForReading();
        }

        /// <summary>
        /// What should be done after Set Data.
        /// </summary>
        protected virtual void AfterSet()
        {
            ExitWriting();
        }


        /// <summary>
        /// Lockless wait until _readingThreads becomes 0.
        /// </summary>
        private void WaitForReading()
        {
            var spinner = new SpinWait();
            while (_readingThreads > 0)
            {
                spinner.SpinOnce();
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

        /// <summary>
        /// Mark a reading starts.
        /// </summary>
        private void EnterReading()
        {
            lock (_memberWritingLock)
            {
                ++_readingThreads;
            }
        }

        /// <summary>
        /// Mark a reading ends.
        /// </summary>
        private void ExitReading()
        {
            lock (_memberWritingLock)
            {
                --_readingThreads;
            }
        }

        /// <summary>
        /// Mark the writing starts.
        /// </summary>
        private void EnterWriting()
        {
            lock (_memberWritingLock)
            {
                this._isWriting = true;
            }
        }

        /// <summary>
        /// Mark the writing ends.
        /// </summary>
        private void ExitWriting()
        {
            lock (_memberWritingLock)
            {
                this._isWriting = false;
            }
        }
    }
}
