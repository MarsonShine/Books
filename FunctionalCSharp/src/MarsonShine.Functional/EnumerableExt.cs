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
        public static void ForEach<R>(this IEnumerable<R> list, Action<R> action)
        {
            foreach (var item in list)
            {
                action(item);
            }
        }

        public static R Match<T, R>(this IEnumerable<T> list, Func<R> Empty, Func<T, IEnumerable<T>, R> Otherwise) => list.Head().Match(
            None: Empty,
            Some: head => Otherwise(head, list.Skip(1))
        );

        public static Option<T> Head<T>(this IEnumerable<T> list) {
            if (list == null) return None;
            var enumerator = list.GetEnumerator();
            return enumerator.MoveNext() ? Some(enumerator.Current) : None;
        }
    }
}
