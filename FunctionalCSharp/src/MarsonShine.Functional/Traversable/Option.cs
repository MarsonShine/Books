namespace MarsonShine.Functional.Traversable
{
    using MarsonShine.Functional;
    using static F;
    public static class OptionTraversable
    {
        public static Exceptional<Option<R>> Traverse<T, R>(this Option<T> tr, Func<T, Exceptional<R>> f) => tr.Match(
            None: () => Exceptional((Option<R>)None),
            Some: t => f(t).Map(Some));

        public static Task<Option<R>> Traverse<T, R>(this Option<T> option, Func<T, Task<R>> f) => option.Match(
            None: () => Async((Option<R>)None),
            Some: t => f(t).Map(Some)
            );
        public static Task<Option<R>> TraverseBind<T, R>(this Option<T> option, Func<T, Task<Option<R>>> f) => option.Match(
            None: () => Async((Option<R>)None),
            Some: t => f(t)
            );
    }
}
