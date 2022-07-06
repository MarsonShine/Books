using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Demo.Examples._11
{
    public static class Instrumentation
    {
        public static T Time<T>(ILogger log, string op, Func<T> f)
        {
            var sw = new Stopwatch();
            sw.Start();

            T t = f();

            sw.Stop();
            log.LogDebug($"{op} took {sw.ElapsedMilliseconds}ms");
            return t;
        }
        public static T Trace<T>(ILogger log, string op, Func<T> f)
        {
            log.LogTrace($"Entering {op}");
            T t = f();
            log.LogTrace($"Leaving {op}");
            return t;
        }

        public static T Trace<T>(Action<string> log, string op, Func<T> f)
        {
            log($"Entering {op}");
            T t = f();
            log($"Leaving {op}");
            return t;
        }

        public static T Time<T>(string op, Func<T> f)
        {
            var sw = new Stopwatch();
            sw.Start();

            T t = f();

            sw.Stop();
            Console.WriteLine($"{op} took {sw.ElapsedMilliseconds}ms");
            return t;
        }
    }
}
