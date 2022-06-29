namespace Demo.Examples._6
{
    using MarsonShine.Functional;
    using static System.Math;
    public class EitherDemo
    {
        public static Either<string, double> Calc(double x, double y)
        {
            if (y == 0) return "y cannot be 0";
            if (x != 0 && Sign(x) != Sign(y))
                return "x / y cannot be negative";
            return Sqrt(x / y);
        }
    }
}
