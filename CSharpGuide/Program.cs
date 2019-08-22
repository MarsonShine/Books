using CSharpGuide.LanguageVersions._8._0;
using System;
using System.Threading.Tasks;

namespace CSharpGuide
{
    class Program
    {
        static void Main(string[] args)
        {
            //new Introducer().Start();
            //_ = await new AsyncStream().ConsumeStream();
            Console.WriteLine("Hello World!");

            MyClass mc = new MyClass();
            M1(mc);
            M2(mc);

            MyClass? local = null;
            MyClass handle = new MyClass {
                MyValue = "marson shine"
            };
            HandleMethods.DisposeAndClear(ref local);   //warning CS8601 可能的 null 引用赋值
            HandleMethods.DisposeAndClear_DisallowNull(ref handle);// 没有警告
            Console.WriteLine(handle.MyValue);
            string[] testArray = new string[] { "Hello!", "World" };
            M(testArray);
            Test();
        }

        public static void M1(MyClass mc)
        {
            mc.MyValue = null;  //没有 AllowNull 标签会有警告
        }

        public static void M2(MyClass mc)
        {
            Console.WriteLine(mc.MyValue.Length);
        }

        public static void M(string[] testArray) {
            //string value = MyArray.Find<string>(testArray, s => s == "Hello");
            string value = MyArray.Find<string>(testArray, s => s == "Hello!");
            Console.WriteLine(value.Length);

            //MyArray.Resize<string>(ref testArray, 200);
            Console.WriteLine(testArray.Length);
        }

        static void Test()
        {
            string? value = "not null";
            var flag = MyString.IsNullOrEmpty(value);
            string? input = "1.0.0.0";
            var flag1 = MyVersion.TryParse(input, out Version? version);
            var queue = new MyQueue<string>();
            queue.Enqueue(value);
            var flag2 = queue.TryDequeue(out value);
            flag2 = queue.TryDequeue(out value);
        }

        static void Test_MaybeNullWhen_NotNullWhen(string? s)
        {
            if (MyString.IsNullOrEmpty(s))
            {
                //这会生成一个警告
                //Console.WriteLine(s.Length);
                return;
            }
            Console.WriteLine(s.Length);    //安全

            if (!MyVersion.TryParse(s, out var version)) {
                //这里有一个警告
                //Console.WriteLine(version.Major);
                return;
            }
            Console.WriteLine(version.Major);
        }

        static void QueueTest(MyQueue<string> q) {
            if (!q.TryDequeue(out string s))
            {
                //警告
                Console.WriteLine(value: s.Length);
                return;
            }
            Console.WriteLine(s.Length);
        }
    }
}
