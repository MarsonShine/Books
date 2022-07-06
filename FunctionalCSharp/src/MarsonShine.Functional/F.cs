using System.Collections.Immutable;
using Unit = System.ValueTuple;

namespace MarsonShine.Functional
{
    public static partial class F
    {
        public static Unit Unit() => default;

        public static Func<T1, Func<T2, R>> Curry<T1, T2, R>(this Func<T1, T2, R> f) => t1 => t2 => f(t1, t2);
        public static Func<T1, Func<T2, Func<T3, R>>> Curry<T1, T2, T3, R>(this Func<T1, T2, T3, R> f) => t1 => t2 => t3 => f(t1, t2, t3);
        public static Func<T1, Func<T2, T3, R>> CurryFirst<T1, T2, T3, R>(this Func<T1, T2, T3, R> f) => t1 => (t2, t3) => f(t1, t2, t3);
        public static Func<T1, Func<T2, T3, T4, R>> CurryFirst<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, R> f) => t1 => (t2, t3, t4) => f(t1, t2, t3, t4);

        public static TResult Using<TDisposable, TResult>(TDisposable disposable, Func<TDisposable, TResult> f)
            where TDisposable : IDisposable
        {
            using (disposable) return f(disposable);
        }

        public static TResult Using<TDisposable, TResult>(Func<TDisposable> disposableFunction, Func<TDisposable, TResult> f)
            where TDisposable : IDisposable
        {
            if (disposableFunction == null)
            {
                throw new ArgumentNullException(nameof(disposableFunction));
            }
            using var disposable = disposableFunction();
            return f(disposable);
        }

        public static IEnumerable<T> List<T>(params T[] items) => items.ToImmutableList();
    }
}
