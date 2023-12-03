using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InMemoryCache
{
    public class ConcurrentSortedDictionary<Tkey, TValue>
        : IDictionary<Tkey,TValue>, IDictionary
        where Tkey: notnull
    {
        public TValue this[Tkey key]
        {
            get
            {
                _lock.WaitForWriting();
                _lock.EnterReading();

                TValue result = 
                throw new NotImplementedException();
                
                _lock.ExitReading();
            }
            set
            {
                _lock.WaitForWriting();
                _lock.EnterWriting();
                _lock.WaitForReading();

                throw new NotImplementedException();

                _lock.ExitWriting();
            }
        }

        private readonly SortedDictionary<Tkey, TValue> _dictionary;
        private readonly ReadWriteLock _lock;


        public ConcurrentSortedDictionary()
        {
            _dictionary = new();
            _lock = new ReadWriteLock();
        }


        /// <summary>
        /// Add if not present
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void Add(Tkey key, TValue value)
        {
            _lock.WaitForWriting();
            _lock.EnterWriting();
            _lock.WaitForReading();

            _dictionary.Add(key, value);

            _lock.ExitWriting();
        }

        public ICollection<Tkey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;



    }
}
