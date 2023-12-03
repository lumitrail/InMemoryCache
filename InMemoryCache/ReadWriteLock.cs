using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryCache
{
    /// <summary>
    /// Multiple read XOR one write.
    /// </summary>
    public class ReadWriteLock
    {
        /// <summary>The number of threads which is currently in reading state./// </summary>
        public int ReadingThreads => _readingThreads;
        /// <summary>Any thread is in writing state.</summary>
        public bool IsWriting => _isWriting;

        private volatile int _readingThreads;
        private readonly object _readingThreadsLock;
        
        private volatile bool _isWriting;
        private readonly object _isWritingLock;
        private readonly object _isWritingAcquireLock;


        public ReadWriteLock()
        {
            _readingThreads = 0;
            _readingThreadsLock = new object();

            _isWriting = false;
            _isWritingLock = new object();
            _isWritingAcquireLock = new object();
        }


        /// <summary>
        /// Lock free wait until _readingThreads becomes 0.
        /// </summary>
        public void WaitForReading()
        {
            var spinner = new SpinWait();
            while (_readingThreads > 0)
            {
                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// Lock free wait while _isWriting == true.
        /// </summary>
        public void WaitForWriting()
        {
            var spinner = new SpinWait();
            while (_isWriting)
            {
                spinner.SpinOnce();
            }
        }


        public void AcquireWriteLock()
        {
            lock (_isWritingAcquireLock)
            {
                lock (_isWritingLock)
                {
                    _isWriting = true;
                }
            }
        }

        public void ReleaseWriteLock()
        {
            lock (_isWritingLock)
            {
                _isWriting = false;
            }
        }

        /// <summary>
        /// Mark a reading starts.
        /// </summary>
        public void EnterReading()
        {
            lock (_readingThreadsLock)
            {
                ++_readingThreads;
            }
        }

        /// <summary>
        /// Mark a reading ends.
        /// </summary>
        public void ExitReading()
        {
            lock (_readingThreadsLock)
            {
                --_readingThreads;
            }
        }
    }
}
