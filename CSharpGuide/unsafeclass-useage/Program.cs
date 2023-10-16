// See https://aka.ms/new-console-template for more information
using System.Runtime.CompilerServices;

// copy from https://twitter.com/neuecc/status/1712737575704916440;
var box = new MagicalBox();
// write
box.TryWrite(100, out var firstOffset);
box.TryWrite(true, out var secondOffset);
box.TryWrite(43.31, out var thirdOffset);
// read
var a = box.Read<int>(firstOffset);
var b = box.Read<bool>(secondOffset);
var c = box.Read<double>(thirdOffset);
Console.WriteLine((a,b,c));

Console.WriteLine("Hello, World!");

public unsafe struct MagicalBox {
    fixed byte storage[128];
    int written;

    public bool TryWrite<T>(T value, out int offset) {
        if(RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            offset = 0;
            return false;
        }

        Unsafe.WriteUnaligned(ref storage[written], value);
        offset = written;
        written += Unsafe.SizeOf<T>();
        return true;
    }

    public T Read<T>(int offset) {
        return Unsafe.ReadUnaligned<T>(ref storage[offset]);
    }
}