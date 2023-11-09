// See https://aka.ms/new-console-template for more information
// https://blog.stephencleary.com/2023/09/memory-mapped-files-overlaid-structs.html

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

byte a = 2;

bool b = Convert.ToBoolean(a);
bool c = AS<byte, bool>(a);
bool c2 = c;
bool d = (b == c2);
Console.WriteLine(Unsafe.SizeOf<byte>());
Console.WriteLine(Unsafe.SizeOf<bool>());
Console.WriteLine("b==c = " + d);

static TTargetValue AS<TSourceValue, TTargetValue>(TSourceValue sourceValue)
    where TSourceValue : struct
    where TTargetValue : struct
{
    var c1 = TypeInfo.GetTypeCode(typeof(TSourceValue));
    var c2 = TypeInfo.GetTypeCode(typeof(TTargetValue));

    if (c1 == TypeCode.DateTime || c2 == TypeCode.DateTime)
    {
        throw new InvalidCastException(
            $"不支持该类型字段的转换： {typeof(TSourceValue).Name}  => {typeof(TTargetValue).Name}");
    }

    if (c1 == c2) return Unsafe.As<TSourceValue, TTargetValue>(ref sourceValue);

    switch (c1)
    {
        case TypeCode.Boolean:
            var v1 = Convert.ToBoolean(sourceValue);
            return Unsafe.As<Boolean, TTargetValue>(ref v1);
        case TypeCode.SByte:
            var v2 = Convert.ToSByte(sourceValue);
            return Unsafe.As<SByte, TTargetValue>(ref v2);
        case TypeCode.Byte:
            var v3 = Convert.ToByte(sourceValue);
            return Unsafe.As<Byte, TTargetValue>(ref v3);
        case TypeCode.Int16:
            var v4 = Convert.ToInt16(sourceValue);
            return Unsafe.As<Int16, TTargetValue>(ref v4);
        case TypeCode.UInt16:
            var v5 = Convert.ToUInt16(sourceValue);
            return Unsafe.As<UInt16, TTargetValue>(ref v5);
        case TypeCode.Int32:
            var v6 = Convert.ToInt32(sourceValue);
            return Unsafe.As<Int32, TTargetValue>(ref v6);
        case TypeCode.UInt32:
            var v7 = Convert.ToUInt32(sourceValue);
            return Unsafe.As<UInt32, TTargetValue>(ref v7);
        case TypeCode.Int64:
            var v8 = Convert.ToInt64(sourceValue);
            return Unsafe.As<Int64, TTargetValue>(ref v8);
        case TypeCode.UInt64:
            var v9 = Convert.ToUInt64(sourceValue);
            return Unsafe.As<UInt64, TTargetValue>(ref v9);
        case TypeCode.Single:
            var v10 = Convert.ToSingle(sourceValue);
            return Unsafe.As<Single, TTargetValue>(ref v10);
        case TypeCode.Double:
            var v11 = Convert.ToDouble(sourceValue);
            return Unsafe.As<Double, TTargetValue>(ref v11);
        case TypeCode.Decimal:
            var v12 = Convert.ToDecimal(sourceValue);
            return Unsafe.As<Decimal, TTargetValue>(ref v12);
        case TypeCode.Char:
            var v13 = Convert.ToChar(sourceValue);
            return Unsafe.As<Char, TTargetValue>(ref v13);
    }

    throw new InvalidCastException(
        $"不支持该类型字段的转换： {typeof(TSourceValue).Name}  => {typeof(TTargetValue).Name}");
}

// example1
using FileStream file = new(@"tmp.dat", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
// 将已有的文件内容按照指定的容量大小进行内存映射文件
using MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(file, null, 1000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, leaveOpen: true);
// 创建视图，实际上是一个句柄（指针）
using MemoryMappedViewAccessor view = mapping.CreateViewAccessor();

// example2
using FileStream file2 = new(@"tmp.dat", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.RandomAccess);
// 将已有的文件内容按照指定的容量大小进行内存映射文件
using MemoryMappedFile mapping2 = MemoryMappedFile.CreateFromFile(file, null, 1000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, leaveOpen: true);
using MemoryMappedViewAccessor view2 = mapping.CreateViewAccessor();
using Overlay overlay = new(view);
ref Data data = ref overlay.As<Data>();
data.First = 1;
data.Second = 2;
// 上面 example2 的代码运行结果，将得到一个长度为 1000 字节的 tmp.dat 文件，其中前四个字节的值为 First (1)，后四个字节的值为 Second (2)。
// 请注意，由于是在内存中读取/写入结构，因此您机器的字节序将决定二进制文件的字节序。继续用十六进制编辑器打开它（有一个在线编辑器叫 HexEd.it），看看二进制文件本身。
// ========================================================


// 在处理跨平台IO方面的问题，字节序（大小端）是绕不开的问题
// 可以通过一些帮助类来实现统一
public static class OverlayHelpers
{
    public static int ReadBigEndian(int bigEndian) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bigEndian) : bigEndian;
    public static int WriteBigEndian(out int bigEndian, int value) => bigEndian = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
    public static int ReadLittleEndian(int littleEndian) =>
      BitConverter.IsLittleEndian ? littleEndian : BinaryPrimitives.ReverseEndianness(littleEndian);
    public static void WriteLittleEndian(out int littleEndian, int value) =>
        littleEndian = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
}

// InlineArrayAttribute Data3
public struct Data3
{
    private int _first;
    private Padding40 _padding;
    private int _second;
    [InlineArray(40)]
    private struct Padding40
    {
        private byte _start;
    }
    // InlineArray 实际上是告诉运行时将该结构中的单个字段重复多次。因此，Padding40 的长度实际上是 40 字节。

    [UnscopedRef]
    public ref T PaddingAs<T>() where T : struct => ref Unsafe.As<Padding40, T>(ref _padding);
    // 上述语法就是利用现有的功能--不安全的固定大小缓冲区--并允许它们在安全的上下文中使用。
    // 如果 < .NET8，则可以使用 unsafe fixed，详见 Data4
}

public unsafe struct Data4
{
    private int _first;
    private fixed byte _padding[40];
    private int _second;

    public unsafe ref T PaddingAs<T>()
    {
        fixed (byte* p = _padding)
            return ref Unsafe.AsRef<T>(p);
    }
}
// 也可以通过更老的方式
[StructLayout(LayoutKind.Explicit)]
public struct Data5
{
    [FieldOffset(0)]
    private int _first;
    [FieldOffset(44)]
    private int _second;
}
[StructLayout(LayoutKind.Sequential)]
public struct Data6
{
    private int _first;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
    private byte[] _padding;
    private int _second;
}
// 如果执行 p/Invoke，这种方法就会起作用，因为它是将结构 marshalling（复制）到非托管代码或从非托管代码复制到非托管代码。由于我们是直接在内存中叠加结构，因此像这样的 marshalling 指令不起作用。

// 当然也可以手动显式字段填充
public struct Data7
{
    private int _first;
    private int _padding0, _padding1, _padding2, _padding3, _padding4, _padding5, _padding6, _padding7, _padding8, _padding9;
    private int _second;
    [UnscopedRef]
    public ref T PaddingAs<T>() where T : struct => ref Unsafe.As<int, T>(ref _padding0);
}
// 所以完整的带有字节序的 Data2 结果如下
public struct Data2
{
    // Layout
    private int _first;
    private int _second;

    public int First
    {
        readonly get => OverlayHelpers.ReadBigEndian(_first);
        set => OverlayHelpers.WriteBigEndian(out _first, value);
    }

    public int Second
    {
        readonly get => OverlayHelpers.ReadBigEndian(_second);
        set => OverlayHelpers.WriteBigEndian(out _second, value);
    }

}
// 重叠结构（overlap structure）
public sealed unsafe class Overlay : IDisposable
{
    private readonly MemoryMappedViewAccessor _view;
    private readonly byte* _pointer;

    public Overlay(MemoryMappedViewAccessor view)
    {
        _view = view;
        view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer); // 使用这种更安全的实现方式，以确保代码的可移植性。（在其它操作系统平台不确保这个 SafeMemoryMappedViewHandle 就是一个指针对象）；
        // 如果确保 SafeMemoryMappedViewHandle 就是一个指针（如 Windows），那么可以使用方式2
    }

    public void Dispose() => _view.SafeMemoryMappedViewHandle?.ReleasePointer();

    public ref T As<T>() where T : struct => ref Unsafe.AsRef<T>(_pointer);
}

// 方式2
public static class MemoryMappedViewAccessorExtensions
{
    public static unsafe ref T As<T>(this MemoryMappedViewAccessor accessor) where T : struct => ref Unsafe.AsRef<T>(accessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer());
}

public struct Data
{
    public int First;
    public int Second;
}
