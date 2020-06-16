using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CSharpGuide;
using CSharpGuide.LanguageVersions._6._0;
using CSharpGuide.LanguageVersions._7._0;
using CSharpGuide.LanguageVersions._8._0;
using CSharpGuide.LanguageVersions.Four.Zero;
using CSharpGuide.random;

namespace CSharpGuide {
    public class Program {
        static void Main(string[] args) {
            Console.WriteLine("*************************均匀分布************************");
            var distribution = StandardContinuousUniform.Distribution.Histogram(0, 1);
            Console.WriteLine(distribution);
            Console.WriteLine("*************************正太分布************************");
            var normalDistribution = Normal.Distribution(1.0, 1.5).Histogram(-4, 4);
            Console.WriteLine(normalDistribution);
            Console.WriteLine("*************************标准离散分布************************");
            var discreteDistribution = StandardDiscreteUniform.Distribution(1, 6)
                .Samples()
                .Take(10)
                .Sum();
            Console.WriteLine(discreteDistribution);
            //new Introducer().Start();
            //_ = await new AsyncStream().ConsumeStream();
            //Console.WriteLine("Hello World!");

            //MyClass mc = new MyClass();
            //M1(mc);
            //M2(mc);

            //MyClass? local = null;
            //MyClass handle = new MyClass {
            //    MyValue = "marson shine"
            //};
            //HandleMethods.DisposeAndClear(ref local);   //warning CS8601 可能的 null 引用赋值
            //HandleMethods.DisposeAndClear_DisallowNull(ref handle);// 没有警告
            //Console.WriteLine(handle.MyValue);
            //string[] testArray = new string[] { "Hello!", "World" };
            //M(testArray);
            //Test();
            ////TestThrowException(null);
            ////Test_DoesNotReturnIf(null);
            //Test_NotNullIfNotNull();

            //var t = ValueTuple.Create(2, 3);
            //Console.WriteLine(t.Item1);
            //Console.WriteLine(t.Item2);
            //Console.WriteLine($"Item1 = {t.Item1}, Item2= ${t.Item2}");

            //元组解构
            var(pd, id) = ValueTuples.Create(2, 3);
            Console.WriteLine(pd);
            Console.WriteLine(id);
            Console.WriteLine($"元组解构：Item1 = {pd}, Item2= ${id}");

            // ITuple tp = new Tuple<int, int>(2, 3);
            // for (int i = 0; i < tp.Length; i++) {
            //     var temp = tp[i];
            // }

            // ITuple p = new ValueTuple<int, int>(1, 2);

            // Example e = new Example();
            // e.Start();

            // // 伪随机
            // FakeRandom fr = new FakeRandom();
            // for (int i = 0; i < 100; i++) {
            //     fr.Invoke();
            // }
            // Console.WriteLine("sizeof(int) = " + sizeof(int));

            Console.WriteLine("*************************TryCatchWhen************************");
            TryCatchWhenExpress.StartTryCatchWhen();

            Console.WriteLine("*************************SwitchCaseWhen************************");
            TryCatchWhenExpress.StartSwitchCaseWhen();

            Console.WriteLine("*************************AsyncLocal via ThreadLocal************************");
            AsyncLocalViaThreadLocal.AsyncMethodA().ConfigureAwait(false).GetAwaiter();
            Console.Read();
        }

        public static void M1(MyClass mc) {
            mc.MyValue = null; //没有 AllowNull 标签会有警告
        }

        public static void M2(MyClass mc) {
            Console.WriteLine(mc.MyValue.Length);
        }

        public static void M(string[] testArray) {
            //string value = MyArray.Find<string>(testArray, s => s == "Hello");
            string value = MyArray.Find<string>(testArray, s => s == "Hello!");
            Console.WriteLine(value.Length);

            //MyArray.Resize<string>(ref testArray, 200);
            Console.WriteLine(testArray.Length);
        }

        public static void M3([DoesNotReturnIf(false)] bool b, string s) {
            if (!b) throw new Exception(s);
        }
        static void Test_DoesNotReturnIf(string? s) {
            M3(s != null, s.ToString());
        }
        static void Test() {
            string? value = "not null";
            var flag = MyString.IsNullOrEmpty(value);
            string? input = "1.0.0.0";
            var flag1 = MyVersion.TryParse(input, out Version? version);
            var queue = new MyQueue<string>();
            queue.Enqueue(value);
            var flag2 = queue.TryDequeue(out value);
            flag2 = queue.TryDequeue(out value);
        }

        static void Test_MaybeNullWhen_NotNullWhen(string? s) {
            if (MyString.IsNullOrEmpty(s)) {
                //这会生成一个警告
                //Console.WriteLine(s.Length);
                return;
            }
            Console.WriteLine(s.Length); //安全

            if (!MyVersion.TryParse(s, out var version)) {
                //这里有一个警告
                //Console.WriteLine(version.Major);
                return;
            }
            Console.WriteLine(version.Major);
        }

        static void Test_NotNullIfNotNull() {
            var s = M5();
            s.ToString();
        }

        [
            return :NotNullIfNotNull("p")
        ]
        static string? M5(params string?[] ? p) => p?.ToString();

        static void QueueTest(MyQueue<string> q) {
            if (!q.TryDequeue(out string s)) {
                //警告
                Console.WriteLine(value: s.Length);
                return;
            }
            Console.WriteLine(s.Length);
        }

        static void PathTest(string? path) {
            var possiblyNullPath = MyPath.GetFileName(path);
            Console.WriteLine(possiblyNullPath.Length); // Warning: Dereference of a possibly null reference

            if (!string.IsNullOrEmpty(path)) {
                var goodPath = MyPath.GetFileName(path);
                Debug.Assert(goodPath != null); //添加断言则不会有警告
                Console.WriteLine(goodPath.Length); // Safe!
            }
        }

        static void TestThrowException(string? args) {
            ThrowHelper.ThrowArgumentNullException(args);

            MyAssertionLibrary.MyAssert(args != null);

        }
    }
}