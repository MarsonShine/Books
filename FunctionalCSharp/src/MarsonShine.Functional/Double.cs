namespace MarsonShine.Functional
{
    using static F;
    public static class Double
    {
        public static Option<double> Parse(string s) => double.TryParse(s, out double result) ? Some(result) : None;
    }
}
