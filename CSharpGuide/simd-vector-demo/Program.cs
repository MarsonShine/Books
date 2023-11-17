// See https://aka.ms/new-console-template for more information
// https://devblogs.microsoft.com/dotnet/hardware-intrinsics-in-net-core/
using simd_vector_demo;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

// 加宽和缩小
byte[] byteBuffer = Enumerable.Range('A', 128 / 8).Select(i => (byte)i).ToArray();
Vector128<byte> byteVector = Vector128.Create(byteBuffer);
Console.WriteLine(byteVector);
(Vector128<ushort> Lower, Vector128<ushort> Upper) = Vector128.Widen(byteVector);
Console.Write(Lower.AsByte());
Console.WriteLine(Upper.AsByte());

Vector256<ushort> ushortVector = Vector256.Create(Lower, Upper);
Console.WriteLine(ushortVector);
Span<ushort> ushortBuffer = stackalloc ushort[256 / 16];
ushortVector.CopyTo(ushortBuffer);
Span<char> charBuffer = MemoryMarshal.Cast<ushort, char>(ushortBuffer);
Console.WriteLine(new string(charBuffer));

Vector256<ushort> ushortVector2 = Vector256.Create((ushort)300);
Console.WriteLine(ushortVector2);
unchecked { Console.WriteLine((byte)300); } // 255超出了byte的范围，所以会溢出；溢出的部分从位置0开始计算，所以结果是44
Console.WriteLine(300 & byte.MaxValue);
Console.WriteLine(ushortVector2.GetLower());
Console.WriteLine(ushortVector2.GetUpper());
Console.WriteLine(VectorOperator.Narrow(ushortVector2.GetLower(), ushortVector2.GetUpper()));

if (Sse2.IsSupported)
{
    Console.WriteLine(Sse2.PackUnsignedSaturate(ushortVector2.GetLower().AsInt16(), ushortVector2.GetUpper().AsInt16()));
}

// 洗牌
Vector128<int> intVector = Vector128.Create(100, 200, 300, 400);
Console.WriteLine(intVector);
// 通过 Shuffle 方法，我们可以将向量中的元素重新排列，下面实现了一个逆序的洗牌
Console.WriteLine(Vector128.Shuffle(intVector, Vector128.Create(3, 2, 1, 0)));