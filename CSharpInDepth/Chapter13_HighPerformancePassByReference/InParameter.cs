using System;
using System.Globalization;

namespace Chapter13_HighPerformancePassByReference
{
    /*
     c#7.2 新增 in 修饰符来修饰参数，代表这个参数所属的方法不会改变这个值，因此变量可以通过引用传递避免值拷贝
    in 修饰符的使用场景（from c# in deep）：
    1. 在有测量以及对性能有意义的地方可以使用 in 参数。特别是涉及到大型结构体的时候。
    2. 除非你的方法功能正确，那么就要避免在公共 API 中使用 in 参数，即使参数值在方法过程中任意改变
    3. 考虑使用公共方法作为防止更改的屏障，然后在私有实现中使用 in 参数以避免复制
    4. 当调用一个方法要传递 in 参数时，就要显式使用 in 参数，除非有意使用编译器通过引用传递隐藏的局部变量。
     */
    public class InParameter
    {
        public static void PrintDateTime(in DateTime value) // 编译器生成 public static void PrintDateTime([In][IsReadOnly] ref DateTime value)
        {
            string text = value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            Console.WriteLine(text);
        }

        public static void Start()
        {
            DateTime start = DateTime.UtcNow;
            PrintDateTime(start);   // 变量是通过隐式的引用传递，等价于下面
            PrintDateTime(in start);
            PrintDateTime(start.AddMinutes(1)); // start.AddMinutes(1) 结果隐式的拷贝到一个本地变量，然后通过引用传递，等价于下面
            DateTime dateTime = start.AddMinutes(1);
            PrintDateTime(in dateTime);

            //PrintDateTime(in start.AddMinutes(1));  // 编译失败，参数无法通过引用传递
            int x = 10;
            InParameters(x, () => x++);
            ValueParameter(x, () => x++);   // 值传递
        }

        static void InParameters(in int p, Action action)
        {
            Console.WriteLine("Start of InParameter method");
            Console.WriteLine($"p = {p}");
            action();
            Console.WriteLine($"p = {p}");
        }
        static void ValueParameter(int p, Action action)
        {
            Console.WriteLine("Start of ValueParameter method");
            Console.WriteLine($"p = {p}");
            action();
            Console.WriteLine($"p = {p}");
        }

        // readonly 只读变量会隐式发生值拷贝
        public  struct YearMonthDay
        {
            public int Year { get;}
            public int Month { get; }
            public int Day { get; }

            public YearMonthDay(int year, int month, int day) => (Year, Month, Day) = (year, month, day);
        }

        public class ImplicitFieldCopy
        {
            private readonly YearMonthDay readOnlyField = new YearMonthDay(2021, 04, 20);
            private YearMonthDay readWriteField = new YearMonthDay(2021, 04, 20);

            public void CheckYear()
            {
                int readOnlyFieldYear = readOnlyField.Year;
                int readWriteFieldYear = readWriteField.Year;
            }

            private void PrintYearMonthDay(YearMonthDay input) => Console.WriteLine($"{input.Year} {input.Month} {input.Day}");
            private void PrintYearMonthDay(in YearMonthDay input) => Console.WriteLine($"{input.Year} {input.Month} {input.Day}");
        }
    }
}
