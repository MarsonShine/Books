namespace Demo.Exercises._4
{
    internal static class Answer
    {
        static ISet<R> Map<T, R>(this ISet<T> set, Func<T, R> func)
        {
            var rs = new HashSet<R>();
            foreach (var s in set)
            {
                rs.Add(func(s));
            }
            return rs;
        }

        static Dictionary<K, R> Map<K, T, R>(this Dictionary<K, T> dic, Func<T, R> func)
            where K : notnull
        {
            var dc = new Dictionary<K, R>();
            foreach (var kv in dic)
            {
                dc.Add(kv.Key, func(kv.Value));
            }
            return dc;
        }
    }
}
