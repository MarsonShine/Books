using System.Collections.Immutable;
using Unit = System.ValueTuple;

namespace MarsonShine.Functional
{
    public static partial class F
    {
        public static Unit Unit() => default;

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
