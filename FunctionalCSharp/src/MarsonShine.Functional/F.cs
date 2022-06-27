using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsonShine.Functional
{
    public static partial class F
    {
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
    }
}
