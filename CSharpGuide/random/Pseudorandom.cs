using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CSharpGuide.random {
    public class Pseudorandom {
        private const int DEFAULT_INT = default(int);
        [NotNull]
        private readonly static ThreadLocal<Random> prng = new ThreadLocal<Random>(() => new Random(BetterRandom.NextInt()));
        public static int NextInt() => prng!.Value!.Next();
        public static int NInt(string? x) {
            Debug.Assert(x != null);
            var split = x.Split(' ');
            if (prng?.Value == null) {
                return DEFAULT_INT;
            } else {
                return prng.Value.Next();
            }
        }

        public static double NextDouble() => prng.Value!.NextDouble();
    }
}