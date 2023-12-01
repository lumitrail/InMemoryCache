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
        : IDictionary<Tkey,TValue>, ICollection<KeyValuePair<Tkey, TValue>>, IEnumerable<KeyValuePair<Tkey, TValue>>, IEnumerable
        where Tkey: notnull, IComparable, IComparable<Tkey>, IEquatable<Tkey>
    {
        private readonly SortedDictionary<Tkey, List<TValue>> _dictionary;

        private volatile int _readingThreads;
        private volatile bool _isWriting;
        private readonly object _memberWritingLock;

        public TValue this[Tkey key]
        {
            get
            {
                throw new NotImplementedException();

            }
            set
            {
                throw new NotImplementedException();
            }
        }



        public ConcurrentSortedDictionary()
        {
            _dictionary = new();
            
            _readingThreads = 0;
            _isWriting = false;
            _memberWritingLock = new object();
        }


        public void Add(Tkey key, TValue value)
        {
            throw new NotImplementedException();
        }

        ICollection<Tkey> Keys
        {
            get
            {
                var result = new Tkey[_dictionary.Count];
                
            }
        }
        ICollection<TValue> Values => _dictionary.Values;

        public void test()
        {
            int[] dda = Array.Empty<int>();
        }


    }
}
