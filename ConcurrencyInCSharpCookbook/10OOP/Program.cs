using System;
using Nito.AsyncEx;

namespace _10OOP {
    class Program {
        static void Main(string[] args) {
            var myClassBuild = AsyncConstructorClass.CreateInstanceAsync();
            Console.WriteLine("before constructor");
            Console.WriteLine(myClassBuild.Result.ToString());
            Console.WriteLine("Hello World!");
        }
    }
}