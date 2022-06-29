using Demo.Examples;

namespace Demo.Exercises._5
{
    internal static class Answer
    {
        static decimal AverageEarningsOfRichestQuartile(List<Person> population) => population
            .OrderByDescending(p => p.Earnings)
            .Take(population.Count / 4)
            .Select(p => p.Earnings)
            .Average();

        static Func<T1, R> Compose<T1, T2, R>(this Func<T2, R> g, Func<T1, T2> f) => x => g(f(x));
    }
}
