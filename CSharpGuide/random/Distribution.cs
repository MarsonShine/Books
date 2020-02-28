using System.Collections.Generic;
using System.Linq;

namespace CSharpGuide.random {
    public static class Distribution {
        public static IEnumerable<T> Samples<T>(this IDistribution<T> d) {
            while (true) {
                yield return d.Sample();
            }
        }

        public static string Histogram(this IDistribution<double> d, double low, double high) => d.Samples().Histogram(low, high);

        public static string Histogram<T>(this IDiscreteDistribution<T> d) where T : notnull => d.Samples().DiscreteHistogram();

        public static string ShowWeights<T>(this IDiscreteDistribution<T> d) where T : notnull {
            int labelMax = d.Support()
                .Select(x => x.ToString() !.Length)
                .Max();

            return string.Join('\n',
                d.Support().Select(x =>
                    $"{ToLabel(x)}:{d.Weight(x)}"));

            string ToLabel(T t) => t.ToString() !.PadLeft(labelMax);
        }
    }
}