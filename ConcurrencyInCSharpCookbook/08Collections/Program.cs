using System;

/// <summary>
/// 不可变集合，值永远无法修改的集合，当要对不可变集合写入修改时，会返回一个新的不可变集合
/// 但并不是说他的内存空间就会翻倍，不可变集合之间通常会共享大部分的存储空间
/// 因此浪费并不大
/// 并且是线程安全的
/// </summary>
namespace _08Collections {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");
        }
    }
}