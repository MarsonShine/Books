namespace CSharpGuide.random {
    using static System.Math;
    using SCU = StandardContinuousUniform;
    public class Normal : IDistribution<double> {
        public double Mean { get; }
        public double Sigma { get; }
        public double μ => Mean;
        public double σ => Sigma;
        public static readonly Normal Standard = Distribution(0, 1);
        public static Normal Distribution(
            double mean, double sigma) => new Normal(mean, sigma);

        public Normal(double mean, double sigma) {
            Mean = mean;
            Sigma = sigma;
        }

        //Box-Muller 算法：将服从均匀分布的随机数转变成服从正态分布的变量
        private double StandardSample() =>
        Sqrt(-2.0 * Log(SCU.Distribution.Sample())) *
        Cos(2.0 * PI * SCU.Distribution.Sample());
        public double Sample() => μ + σ * StandardSample();
    }
}