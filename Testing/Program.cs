using InMemoryCache.TestingCodes;

namespace Testing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await SimpleTesting.InMemoryCacheTest();
        }
    }
}
