using InMemoryCache;

Console.WriteLine("Hello, World!");
var intToIntCache = new InMemoryCache<int, int>(1024);
var intToStringCache = new InMemoryCache<int, string>(1024);
var stringToIntCache = new InMemoryCache<string, int>(1024);
var intToClassCache = new InMemoryCache<int, MySampleClass>(1024);


intToClassCache.TryWrite(1, null);

if (intToClassCache.TryRead(1, out MySampleClass outValue))
{
    Console.WriteLine(outValue.ToString());
}


public class MySampleClass
{
    public int Id { get; }

    public MySampleClass(int id)
    {
        Id = id;
    }
}
