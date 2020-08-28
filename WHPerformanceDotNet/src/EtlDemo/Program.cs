using System;

namespace EtlDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Events.Log.ProcessingStart();
            Events.Log.FoundPrime(7);
            Console.ReadLine();
        }
    }
}
