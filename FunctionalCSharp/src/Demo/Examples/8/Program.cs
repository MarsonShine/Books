using MarsonShine.Functional;

namespace Demo.Examples._8
{
    using static F;
    public class Program
    {
        public static void Main()
        {
            Func<int, int, int> multiply = (x, y) => x * y;
            Some(3).Map(multiply).Apply(Some(4));
            Some(3).Map(multiply).Apply(None);
        }
    }
}
