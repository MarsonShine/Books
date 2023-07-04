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

Vector128<float> left2 = Vector128.Create(1.0f,2,3,4);
Vector128<float> right2 = Vector128.Create(4.0f,3,2,1);
Vector128<float> r = Vector128.GreaterThan(left2,right2);
Vector128<float> result = Vector128.ConditionalSelect(r,left2,right2);

Console.WriteLine(Vector128.Create(4.0f,3,3,4) == result);
