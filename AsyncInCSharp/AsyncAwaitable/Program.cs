using System;
using System.Threading.Tasks;

namespace AsyncAwaitable {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }

        private async Task Test() {
            var t = new Task(() => {
                Console.WriteLine("customer awaitable");
            });
            await new ExampleAwaitable(t);
        }
    }
}