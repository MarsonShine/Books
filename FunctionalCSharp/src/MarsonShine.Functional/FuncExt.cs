using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unit = System.ValueTuple;

namespace MarsonShine.Functional
{
    using static F;
    public static class FuncExt
    {
        public static Func<T> ToNullary<T>(this Func<Unit, T> f)
          => () => f(Unit());

        public static Func<T1, R> Compose<T1, T2, R>(this Func<T2, R> g, Func<T1, T2> f)
           => x => g(f(x));

        public static Func<T2, R> Apply<T1, T2, R>(this Func<T1, T2, R> f, T1 t1) => t2 => f(t1, t2);
        public static Func<T2, T3, R> Apply<T1, T2, T3, R>(this Func<T1, T2, T3, R> f, T1 t1) => (t2, t3) => f(t1, t2, t3);
    }
}
