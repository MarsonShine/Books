using CSharpGuide.LanguageVersions._8._0;
using System;
using System.Threading.Tasks;

namespace CSharpGuide
{
    class Program
    {
        static async Task Main(string[] args)
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

        }

        public static void M1(MyClass mc)
        {
            mc.MyValue = null;  //没有 AllowNull 标签会有警告
        }

        public static void M2(MyClass mc)
        {
            Console.WriteLine(mc.MyValue.Length);
        }
    }
}
