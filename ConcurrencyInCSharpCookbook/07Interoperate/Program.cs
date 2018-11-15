using System;

namespace _07Interoperate {
    class Program {
        static void Main(string[] args) {
            UseRxObservableEncapsulateAsyncCode.UseStartAsync();
            var response = UseRxObservableEncapsulateAsyncCode.UseFromAsync();
            response.Subscribe();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}