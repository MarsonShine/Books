using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        // 离散图
        public static string DiscreteHistogram<T>(this IEnumerable<T> d)
        where T : notnull {
            const int sampleCount = 100000;
            const int width = 40;
            var dict = d.Take(sampleCount)
                .GroupBy(x => x)
                .ToDictionary(g => g.Key, g => g.Count());
            int labelMax = dict.Keys
                .Select(x => x.ToString() !.Length)
                .Max();
            var sup = dict.Keys.OrderBy(x => x).ToList();
            int max = dict.Values.Max();
            double scale = max < width ? 1.0 : ((double) width) / max;

            return string.Join(
                "\n",
                sup.Select(s => $"{ToLabel(s)}|{Bar(s)}"));
            // local method
            string ToLabel(T t) => t.ToString() !.PadLeft(labelMax);
            string Bar(T t) =>
                new string('*', (int) (dict[t] * scale));
        }
    }
}