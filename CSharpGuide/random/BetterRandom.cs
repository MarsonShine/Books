using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CRNG = System.Security.Cryptography.RandomNumberGenerator;

namespace CSharpGuide.random {
    public static class BetterRandom {
        private static readonly ThreadLocal<CRNG> crng = new ThreadLocal<CRNG>(CRNG.Create);
        private static readonly ThreadLocal<byte[]> bytes = new ThreadLocal<byte[]>(() => new byte[sizeof(int)]);
        public static int NextInt() {
            crng?.Value?.GetBytes(bytes.Value);
            return BitConverter.ToInt32(bytes.Value!, 0) & int.MaxValue;
        }
        public static double NextDouble() {
            while (true) {
                long x = NextInt() & 0x007FFFFF; //通过随机数与0x7FFFFF进行与操作，来消除高位
                x <<= 31;
                x |= (long) NextInt();
                double n = x;
                const double d = 1L << 52; //double 64位，1符号为 11阶码位 52小数位
                double q = n / d;
                if (q != 1.0)
                    return q;
            }
        }
    }
}