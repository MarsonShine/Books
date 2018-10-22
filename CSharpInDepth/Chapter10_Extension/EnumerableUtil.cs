using System;
using System.Collections;
using System.Collections.Generic;

namespace Chapter10_Extension {
    public static class EnumerableUtil {
        public static IEnumerable<T> Where<T> (this IEnumerable<T> source,
            Func<T, bool> predicate) {
            if (source == null || predicate == null)
                throw new ArgumentNullException ();
            return WhereImpl (source, predicate);
        }

        private static IEnumerable<T> WhereImpl<T> (IEnumerable<T> source, Func<T, bool> predicate) {
            foreach (T item in source) {
                if (predicate (item)) {
                    yield return item;
                }
            }
        }
    }
}