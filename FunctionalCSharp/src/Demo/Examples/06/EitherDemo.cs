namespace Demo.Examples._6
{
    using MarsonShine.Functional;
    using static System.Math;
    using static MarsonShine.Functional.F;
    public class EitherDemo
    {
        public static Either<string, double> Calc(double x, double y)
        {
            if (y == 0) return "y cannot be 0";
            if (x != 0 && Sign(x) != Sign(y))
                return "x / y cannot be negative";
            return Sqrt(x / y);
        }

        public static Either<Error, int> Run(double x, double y) => 
            Calc(x, y).Map(
                left: msg => Error(msg), 
                right: d => d)
            .Bind(ToIntIfWhole);

        static Either<Error, int> ToIntIfWhole(double d)
        {
            if ((int)d == d) return (int)d;
            return Error($"Expected a whole number but got {d}");
        }
    }
}
