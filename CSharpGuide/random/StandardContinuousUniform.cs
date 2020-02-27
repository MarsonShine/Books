namespace CSharpGuide.random {
    using SCU = StandardContinuousUniform;
    /// <summary>
    /// 标准连续均匀
    /// </summary>
    public class StandardContinuousUniform : IDistribution<double> {
        public static readonly StandardContinuousUniform Distribution = new StandardContinuousUniform();

        private StandardContinuousUniform() { }

        public double Sample() => Pseudorandom.NextDouble();
    }
}