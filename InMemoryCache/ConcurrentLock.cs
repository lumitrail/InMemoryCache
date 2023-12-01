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
    public class ConcurrentLock
    {
        /// <summary>The number of threads which is currently in reading state./// </summary>
        public int ReadingThreads => _readingThreads;
        /// <summary>The number of threads which is currently in writing state.</summary>
        public int WritingThreads => _writingThreads;

        private volatile int _readingThreads;
        private volatile int _writingThreads;
        private readonly object _memberAccessLock;


        public ConcurrentLock()
        {
            _readingThreads = 0;
            _writingThreads = 0;
            _memberAccessLock = new object();
        }


        /// <summary>
        /// Lockless wait until _readingThreads becomes 0.
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
        /// Lockless wait until _writingThreads becomes 0.
        /// </summary>
        public void WaitForWriting()
        {
            var spinner = new SpinWait();
            while (_writingThreads > 0)
            {
                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// Mark a reading starts.
        /// </summary>
        public void EnterReading()
        {
            lock (_memberAccessLock)
            {
                ++_readingThreads;
            }
        }

        /// <summary>
        /// Mark a reading ends.
        /// </summary>
        public void ExitReading()
        {
            lock (_memberAccessLock)
            {
                --_readingThreads;
            }
        }

        /// <summary>
        /// Mark the writing starts.
        /// </summary>
        public void EnterWriting()
        {
            lock (_memberAccessLock)
            {
                ++_writingThreads;
            }
        }

        /// <summary>
        /// Mark the writing ends.
        /// </summary>
        public void ExitWriting()
        {
            lock (_memberAccessLock)
            {
                --_writingThreads;
            }

        }
    }
}
