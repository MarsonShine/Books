namespace MarsonShine.Functional.Traversable
{
    using static F;
    using MyExceptional = Exceptional;
    public static class IEnumerableTraversable
    {
        static Func<IEnumerable<T>, T, IEnumerable<T>> Append<T>() => (ts, t) => ts.Append(t);

        public static Exceptional<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> list, Func<T, Exceptional<R>> f) => list.Aggregate(
            seed: MyExceptional.Of(Enumerable.Empty<R>()),
            func: (optRs, t) => from rs in optRs
                                from r in f(t)
                               select rs.Append(r));

        public static Option<IEnumerable<R>> Traverse<T, R>(this IEnumerable<T> list, Func<T, Option<R>> f) => list.Aggregate(
            seed: Some(Enumerable.Empty<R>()),
            func: (optRs, t) => Some(Append<R>()).Apply(optRs).Apply(f(t)));
    }
}
