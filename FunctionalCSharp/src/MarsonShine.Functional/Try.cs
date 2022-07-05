using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsonShine.Functional
{
    public delegate Exceptional<T> Try<T>();

    public static partial class F
    {
        public static Try<T> Try<T>(Func<T> f) => () => f();
    }
    public static class TryExt
    {
        public static Exceptional<T> Run<T>(this Try<T> @try)
        {
            try
            {
                return @try();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public static Try<R> Map<T, R>(this Try<T> @try, Func<T, R> f) => () => @try.Run()
        .Match<Exceptional<R>>(
            ex => ex,
            t => f(t)
            );

        // 组合多个Try
        public static Try<R> Bind<T, R>(this Try<T> @try, Func<T, Try<R>> f) => () => @try.Run().Match(
            ex => ex,
            t => f(t).Run());
    }
}
