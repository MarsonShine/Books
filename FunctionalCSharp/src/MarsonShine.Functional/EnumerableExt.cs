namespace MarsonShine.Functional
{
    using System.Collections.Immutable;
    using static F;
    public static class EnumerableExt
    {
        public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, IEnumerable<R>> func)
         => list.SelectMany(func);

        public static IEnumerable<R> Bind<T, R>(this IEnumerable<T> list, Func<T, Option<R>> func)
          => list.Bind(t => func(t).AsEnumerable());

        public static IEnumerable<R> Map<T, R>(this IEnumerable<T> list, Func<T, R> func) => list.Select(func);
    }
}
