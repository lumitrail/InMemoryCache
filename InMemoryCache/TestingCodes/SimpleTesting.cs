using System.Diagnostics;

namespace InMemoryCache.TestingCodes
{
    internal class SimpleTesting
    {
        public static async Task InMemoryCacheTest()
        {
            var intToIntCache = new InMemoryCache<int, int>();
            var intToIntCacheWithCapacity = new InMemoryCache<int, int>(16);
            var intToIntCacheWithCapacityAndLifeSpan = new InMemoryCache<int, int>(16, TimeSpan.FromSeconds(5));
            var intToStringCache = new InMemoryCache<int, string>();
            var intToClassCache = new InMemoryCache<int, MySampleClass>();

            Console.WriteLine("Initial reports:");
            Console.WriteLine(GetReport(intToIntCache));
            Console.WriteLine(GetReport(intToIntCacheWithCapacity));
            Console.WriteLine(GetReport(intToIntCacheWithCapacityAndLifeSpan));
            Console.WriteLine(GetReport(intToStringCache));
            Console.WriteLine(GetReport(intToClassCache));

            // int to int cache********************************************************************
            Console.WriteLine("\nNormal Writing: write 16 elements. value=key+1");
            for (int i = 0; i < 16; ++i)
            {
                Debug.Assert(intToIntCache.TrySetValue(i, i + 1));
            }
            //Console.WriteLine("Normal Writing Errors:");
            Console.WriteLine(GetReport(intToIntCache));
            for (int i = 0; i < 16; ++i)
            {
                bool isGetSuccess = intToIntCache.TryGetValue(i, out int value);
                Debug.Assert(isGetSuccess);
                if (isGetSuccess)
                {
                    Debug.Assert(value == i + 1);
                }
            }

            Console.WriteLine("\nOverwriting: write 17 elements. value=key+2");
            for (int i = 0; i < 17; ++i)
            {
                Debug.Assert(intToIntCache.TrySetValue(i, i + 2));
            }
            //Console.WriteLine("Overwriting Errors:");
            Console.WriteLine(GetReport(intToIntCache));
            for (int i = 0; i < 17; ++i)
            {
                bool isGetSuccess = intToIntCache.TryGetValue(i, out int value);
                Debug.Assert(isGetSuccess);
                if (isGetSuccess)
                {
                    Debug.Assert(value == i + 2);
                }
            }

            Console.WriteLine("\nWriting beyond capacity: write 17 elements. value=key+1");
            for (int i = 0; i < 17; ++i)
            {
                Debug.Assert(intToIntCacheWithCapacity.TrySetValue(i, i + 1));
            }
            //Console.WriteLine("Writing beyond capacity Errors:");
            Debug.Assert(!intToIntCacheWithCapacity.TryGetValue(0, out _));
            for (int i = 1; i < 17; ++i)
            {
                bool isGetSuccess = intToIntCacheWithCapacity.TryGetValue(i, out int value);
                Debug.Assert(isGetSuccess);
                if (isGetSuccess)
                {
                    Debug.Assert(value == i + 1);
                }
            }

            Console.WriteLine("\nWriting testing life span: write 16 elements. value=key+1");
            for (int i = 0; i < 16; ++i)
            {
                Debug.Assert(intToIntCacheWithCapacityAndLifeSpan.TrySetValue(i, i + 1));
            }
            await Task.Delay(5050);
            //Console.WriteLine("Writing testing life span Errors:");
            for (int i = 0; i < 16; ++i)
            {
                Debug.Assert(!intToIntCacheWithCapacityAndLifeSpan.TryGetValue(i, out _));
            }

            // int to string cache*****************************************************************
            // int to class cache******************************************************************
            Console.WriteLine("TEST DONE.");
        }

        private static string GetReport<Tkey, TValue>(InMemoryCache<Tkey, TValue> cache)
        {
            return $@"{nameof(cache)}: capacity={cache.Capacity}, life span={cache.ElementLifeSpan.TotalSeconds}sec, count={cache.Count()}";
        }

        public class MySampleClass
        {
            public int Id { get; }

            public MySampleClass(int id)
            {
                Id = id;
            }
        }
    }
}
