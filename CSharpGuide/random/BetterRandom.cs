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
    }
}