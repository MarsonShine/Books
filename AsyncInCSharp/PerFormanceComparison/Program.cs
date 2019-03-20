using System;

namespace PerFormanceComparison {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            AsyncAndSyncComparison comparison = new AsyncAndSyncComparison();
            comparison.Start();
        }
    }
}