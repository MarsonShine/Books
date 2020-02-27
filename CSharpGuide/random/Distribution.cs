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
    }
}