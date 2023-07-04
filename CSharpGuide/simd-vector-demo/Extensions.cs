using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace simd_vector_demo
{
    public static class Extensions
    {
        public static T Sum<T>(this IEnumerable<T> source)
            where T: struct, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            if (source.GetType() == typeof(T[]))
                return Sum(Unsafe.As<T[]>(source));

            if (source.GetType() == typeof(List<T>))
                return Sum<T>(CollectionsMarshal.AsSpan(Unsafe.As<List<T>>(source)));

            var sum = T.AdditiveIdentity;
            foreach (var value in source)
            {
                checked
                {
                    sum += value;
                }
            }
            return sum;
        }

        public static T Sum<T>(this T[] source)
            where T : struct, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T> => Sum<T>(source.AsSpan());

        private static T Sum<T>(this ReadOnlySpan<T> source) where T : struct, IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
        {
            var sum = T.AdditiveIdentity;
            // 当使用向量化操作计算时，需要将大量数据划分为块。这些块的大小由底层硬件指令设置。在 .NET 中，通常将块大小设置为 Vector<T>.Count。
            // 所以代码首先检查 source.Length 是否大于 Vector<T>.Count。如果是，则进行向量化操作，利用硬件并行计算来加速。
            if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && source.Length > Vector<T>.Count)
            {
                var vectors = MemoryMarshal.Cast<T, Vector<T>>(source); 
                var sumVector = Vector<T>.Zero;
                foreach (var vector in vectors)
                    sumVector += vector;
                sum = Vector.Sum(sumVector);
                // 向量化操作后，可能会剩余一些数据。这些数据不足以构成一个向量，所以需要使用标量操作。
                var remainder = source.Length % Vector<T>.Count;
                source = source[^remainder..];
            }
            // 否则使用标量操作。
            foreach (ref readonly var value in source)
            {
                checked
                {
                    sum += value;
                }
            }
            return sum;
        }
    }
}
