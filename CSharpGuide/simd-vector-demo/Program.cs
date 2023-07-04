// See https://aka.ms/new-console-template for more information
using simd_vector_demo;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

Console.WriteLine("Hello, World!");

var source = new List<int>(Enumerable.Range(0, 100));
Console.WriteLine(Extensions.Sum(source));

Span<int> buffer = new int[2] { 1, 2 };
nuint oneVectorAwayFromEnd = (nuint)(buffer.Length - Vector128<int>.Count);
Console.WriteLine(oneVectorAwayFromEnd.ToString("X"));

Vector128<int> left = Vector128.Create(1, 2, 3, 4);
Vector128<int> right = Vector128.Create(0, 0, 3, 0);
Vector128<int> equals = Vector128.Equals(left, right);
Console.WriteLine(equals);