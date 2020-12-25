using System;
using System.Runtime.InteropServices;

namespace ncore
{
    class Program
    {
        [DllImport(@"E:\repositories\Books\CSAPP\cpps\csapp\ncore\bin\Debug\netcoreapp3.1\mathlib.dll", EntryPoint =
       "math_add", CallingConvention = CallingConvention.StdCall)]
        public static extern int Add(int a, int b);
        static void Main(string[] args)
        {
            int result = Add(1, 2);
            Console.WriteLine("result is {0}", result);
            //Halts the program
            Console.ReadKey();
        }
    }
}
