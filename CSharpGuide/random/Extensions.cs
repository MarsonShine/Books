using System.Collections.Generic;
using System.Linq;

namespace CSharpGuide.random {
    public static class Extensions {
        public static string Histogram(this IEnumerable<double> d, double low, double high) {
            const int width = 40;
            const int height = 20;
            const int sampleCount = 10000;
            int[] buckets = new int[width];
            foreach (double c in d.Take(sampleCount)) {
                int bucket = (int) (buckets.Length * (c - low) / (high - low));
                if (0 <= bucket && bucket < buckets.Length)
                    buckets[bucket] += 1;
            }
            int max = buckets.Max();
            double scale = max < height ? 1 : ((double) height) / max;
            return string.Join("",
                Enumerable.Range(0, height).Select(
                    r => string.Join("", buckets.Select(
                        b => b * scale > (height - r) ? '*' : ' '
                    )) + "\n"
                )) + new string('-', width) + "\n";
        }
    }
}