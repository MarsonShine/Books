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
            _ = await new AsyncStream().ConsumeStream();
            Console.WriteLine("Hello World!");
        }
    }
}
