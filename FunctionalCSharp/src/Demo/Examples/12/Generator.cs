using NUnit.Framework;

namespace Demo.Examples._12
{
    public delegate (T Value, int Seed) Generator<T>(int seed);

    public class Generator
    {
        public static Generator<int> NextInt = (seed) =>
        {
            seed ^= (seed >> 13);
            seed ^= (seed << 18);
            int result = seed & 0x7fffffff;
            return (result, result);
        };

        //public static Generator<char> NextChar = from i in NextInt select (char)(i % (char.MaxValue + 1));
        [Test]
        public void GeneratorTest()
        {
            var value = NextInt(1000).Value;
            Assert.NotZero(value);
        }
    }
}
