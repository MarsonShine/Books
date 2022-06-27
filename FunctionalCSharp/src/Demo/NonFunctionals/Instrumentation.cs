using System.Diagnostics;


namespace Demo.NonFunctionals
{
    public static class Instrumentation
    {
        // 这个方法签名限定了只能是Func<T>输入，T类型的输出
        // 如果需要无返回值的Time，就只能重载
        public static T Time<T>(string op, Func<T> f) {
            var sw = new Stopwatch();
            sw.Start();
            T t = f();
            sw.Stop();
            WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
            return t;
        }
        // 实际上这里面的代码绝大部分是重复的，那么如何才能避免这种情况呢
        // 这就需要我们自定义一个void类型（函数式编程思想）
        public static void Time<T>(string op, Action f)
        {
            var sw = new Stopwatch();
            sw.Start();
            f();
            sw.Stop();
            WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
        }
    }
}