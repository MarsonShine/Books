using System;
using System.Text;

namespace Chapter12_Deconstruction_PatnerMatching
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            StringBuilder builder = new("12345");
            StringBuilder original = builder;
            //(builder, builder.Length) = (new StringBuilder("67890"), 3);

            //Console.WriteLine(original);
            //Console.WriteLine(builder);

            StringBuilder targetForLength = builder;
            (StringBuilder, int) tuple = (new StringBuilder("67890"), 3);
            builder = tuple.Item1;
            targetForLength.Length = tuple.Item2;
            Console.WriteLine(original);
            Console.WriteLine(builder);

            var point = new Point(1.5, 20);
            var (x, y) = point;
            Console.WriteLine($"x = {x}");
            Console.WriteLine($"x = {y}");

            // 拓展函数
            DateTime birthday = new DateTime(1993, 10, 03);
            DateTime now = DateTime.UtcNow;
            var (year, month, day, hour, minute, second) = now;
            // 解构的二义性错误
            //(string yearString, int month1, int day1) = birthday;


            // 模式匹配
            PatnerMatching.ConstPatnerMatch("hello");
            PatnerMatching.ConstPatnerMatch(5L);
            PatnerMatching.ConstPatnerMatch(7);
            PatnerMatching.ConstPatnerMatch(10);
            PatnerMatching.ConstPatnerMatch(10L);   // 匹配不到

            PatnerMatching.Match(10L);  // 此处传参会装箱为 object，当比较 object is 10 就会不成立
            long xl = 10L;
            if (xl is 10)
            {
                Console.WriteLine("x is 10L");
            }

            PatnerMatching.CheckType<int?>(null);
            PatnerMatching.CheckType<int?>(5);
            PatnerMatching.CheckType<int?>("text");
            PatnerMatching.CheckType<string>(null);
            PatnerMatching.CheckType<string>(5);
            PatnerMatching.CheckType<string>("text");
            Console.ReadLine();
        }
    }

    public sealed class Point
    {
        public double X { get; }
        public double Y { get; }
        // 解构
        public Point(double x, double y) => (X, Y) = (x, y);

        // 函数解构
        public void Deconstruct(out double x, out double y)
        {
            x = X;
            y = Y;
        }
    }
}
