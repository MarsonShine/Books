using System.Runtime.CompilerServices;
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
    }
}
