using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace _10OOP {
    class Program {
        static void Main(string[] args) {
            var myClassBuild = AsyncConstructorClass.CreateInstanceAsync();
            Console.WriteLine("before constructor");
            Console.WriteLine(myClassBuild.Result.ToString());

            DisposedAsync disposedAsync = new DisposedAsync();
            AsyncContext.Run(() => Test());
            Console.WriteLine("Hello World!");
        }

        public static async Task Test() {
            Task<int> task;
            using(var resource = new DisposedAsync()) {
                task = resource.GetDataAsync();
            }
            var ret = await task;
        }
    }
}