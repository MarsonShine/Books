using Demo.Examples._8.Immutables;
using MarsonShine.Functional;
using NUnit.Framework;

namespace Demo.Examples._8
{
    using static F;
    using MyDouble = MarsonShine.Functional.Double;
    public class Program
    {
        public static void Main()
        {
            Func<int, int, int> multiply = (x, y) => x * y;
            Some(3).Map(multiply).Apply(Some(4));
            Some(3).Map(multiply).Apply(None);

            // associative law: (a + b) + c == a + (b + c)
            // (m >>= f) >>= g == m >>= (f >>= g) 
            // 编码表达式：(m >>= f) >>= g == m >>= (x >>= f(x) >>= g)

            // m.Bind(x => f(x).Bind(y => g(y)))

            // 不可变
            var @as = new AccountState(new Currency());

        }
        [Test]
        public void TestAssociativeLaw()
        {
            Func<double, Option<double>> safeSqrt = d => d < 0 ? None : Some(Math.Sqrt(d));
            var opt = Some<string>("123");
            Assert.AreEqual(
                opt.Bind(MyDouble.Parse).Bind(safeSqrt),
                opt.Bind(x => MyDouble.Parse(x).Bind(safeSqrt))
                );
        }
    }
}
