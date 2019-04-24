using System;

namespace WitchThreadRunCode {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
            AnalyzeAwaiting analyze = new AnalyzeAwaiting();
            analyze.Button_Click().ContinueWith(t => {
                Console.WriteLine("Ending");
            });
            Console.ReadLine();
        }
    }
}