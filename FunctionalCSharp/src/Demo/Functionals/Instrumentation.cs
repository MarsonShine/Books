using System.Diagnostics;
using MarsonShine.Functional;
using Unit = System.ValueTuple; // 通过元祖来表示”无返回值“

namespace Demo.Functionals
{
    // 如果你想在一个函数中需要传入一个Func，但你希望使用一个Action
    // 这个时候就需要用自定义返回类型（通过适配器模式）
    public static class Instrumentation
    {
        public static void Time(string op, Action act) => Time<Unit>(op, act.ToFunc());
        public static T Time<T>(string op, Func<T> f) {
            var sw = new Stopwatch();
            sw.Start();
            T t = f();
            sw.Stop();
            WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
            return t;
        }
    }
}