using System;

namespace LockFreeWithInterlocked {
    class Program {
        static void Main(string[] args) {
            LockFreeStack<string> lockFree = new LockFreeWithInterlocked.LockFreeStack<string>();
            lockFree.Push("marson shine");
            lockFree.Push("summer zhu");

            string name = lockFree.Pop();
            Console.WriteLine("Hello World!");
        }
    }
}