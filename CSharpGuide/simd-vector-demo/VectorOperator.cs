using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace simd_vector_demo
{
    internal class VectorOperator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidAscii(Vector128<byte> vector)
        {
            // 为了进行">127"检查，我们可以使用GreaterThanAny方法：
            return !Vector128.GreaterThanAny(vector, Vector128.Create((byte)127));
            // 为了进行"<0 "检查，我们需要使用AsSByte和LessThanAny方法：
            return !Vector128.LessThanAny(vector.AsSByte(), Vector128<sbyte>.Zero);
            // 要进行AND操作，我们需要使用操作符&。
            return (vector & Vector128.Create((byte)0b_1000_0000)) == Vector128<byte>.Zero;
            // 也可以使用 ExtractMostSignificantBits 方法
            return vector.ExtractMostSignificantBits() == 0;
        }

        public static unsafe Vector128<byte> Narrow(Vector128<ushort> lower, Vector128<ushort> upper)
        {
            Unsafe.SkipInit(out Vector128<byte> result);

            for (int i = 0; i < Vector128<ushort>.Count; i++)
            {
                byte value = (byte)GetElementUnsafe(lower, i);
                SetElementUnsafe(result, i, value);
            }

            for (int i = Vector128<ushort>.Count; i < Vector128<byte>.Count; i++)
            {
                byte value = (byte)GetElementUnsafe(upper, i - Vector128<ushort>.Count);
                SetElementUnsafe(result, i, value);
            }

            return result;
        }

        static T GetElementUnsafe<T>(in Vector128<T> vector, int index)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector128<T>.Count));
            return Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in vector)), index);
        }

        static void SetElementUnsafe<T>(in Vector128<T> vector, int index, T value)
            where T : struct
        {
            Debug.Assert((index >= 0) && (index < Vector128<T>.Count));
            Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref Unsafe.AsRef(in vector)), index) = value;
        }

        static unsafe bool Contains(ReadOnlySpan<byte> haystack,byte needle)
        {
            if(Vector128.IsHardwareAccelerated &&haystack.Length >= Vector128<byte>.Count)
            {
                ref byte current = ref MemoryMarshal.GetReference(haystack);
#if NET8_0
                if(Vector512.IsHardwareAccelerated && haystack.Length >= Vector512<byte>.Count)
                {
                    Vector512<byte> target = Vector512.Create(needle);
                    ref byte endMinusOneVector = ref Unsafe.Add(ref current, haystack.Length - Vector512<byte>.Count);
                    do
                    {
                        if (Vector512.EqualsAny(target, Vector512.LoadUnsafe(ref current)))
                            return true;

                        current = ref Unsafe.Add(ref current, Vector512<byte>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref current, ref endMinusOneVector));

                    if (Vector512.EqualsAny(target, Vector512.LoadUnsafe(ref endMinusOneVector)))
                        return true;
                }
#endif
                if(Vector256.IsHardwareAccelerated && haystack.Length >= Vector256<byte>.Count)
                {
                    Vector256<byte> target = Vector256.Create(needle);
                    ref byte endMinusOneVector = ref Unsafe.Add(ref current, haystack.Length - Vector256<byte>.Count);
                    do
                    {
                        if (Vector256.EqualsAny(target, Vector256.LoadUnsafe(ref current)))
                            return true;

                        current = ref Unsafe.Add(ref current, Vector256<byte>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref current, ref endMinusOneVector));

                    // 剩下的
                    if (Vector256.EqualsAny(target, Vector256.LoadUnsafe(ref endMinusOneVector)))
                        return true;
                }
                else
                {
                    Vector128<byte> target = Vector128.Create(needle);
                    ref byte endMinusOneVector = ref Unsafe.Add(ref current, haystack.Length - Vector128<byte>.Count);
                    do
                    {
                        if (Vector128.EqualsAny(target, Vector128.LoadUnsafe(ref current)))
                            return true;

                        current = ref Unsafe.Add(ref current, Vector128<byte>.Count);
                    }
                    while (Unsafe.IsAddressLessThan(ref current, ref endMinusOneVector));

                    if (Vector128.EqualsAny(target, Vector128.LoadUnsafe(ref endMinusOneVector)))
                        return true;
                }

            }
            // 不支持向量，就进行标量操作
            else
            {
                for (int i = 0; i < haystack.Length; i++)
                    if (haystack[i] == needle)
                        return true;
            }
            return false;
        }
    }
}
