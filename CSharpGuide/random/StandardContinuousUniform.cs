namespace CSharpGuide.random {
    using System.Collections.Generic;
    using System;
    using SCU = StandardContinuousUniform;
    using System.Linq;

    /// <summary>
    /// 标准连续均匀
    /// </summary>
    public class StandardContinuousUniform : IDistribution<double> {
        public static readonly StandardContinuousUniform Distribution = new StandardContinuousUniform();

        private StandardContinuousUniform() { }

        public double Sample() => Pseudorandom.NextDouble();
    }

    public class StandardDiscreteUniform : IDiscreteDistribution<int> {
        public int Min { get; }
        public int Max { get; }

        public StandardDiscreteUniform(int min, int max) {
            this.Min = min;
            this.Max = max;
        }

        public static StandardDiscreteUniform Distribution(int min, int max) {
            if (min > max)
                throw new ArgumentException();
            return new StandardDiscreteUniform(min, max);
        }
        public int Sample() => (int) ((SCU.Distribution.Sample() * (1.0 + Max - Min)) + Min);

        public IEnumerable<int> Support() => Enumerable.Range(Min, 1 + Max - Min);

        public int Weight(int i) => (Min <= i && i <= Max) ? 1 : 0;

        public override string ToString() => $"StandardDiscreteUniform[{Min}, {Max}]";
    }
}