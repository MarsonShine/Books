using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Magics
{
    public static class AdaptorExtensions
    {
        public static Func<T2, T1, TResult> SwapArgs<T1, T2, TResult>(this Func<T1, T2, TResult> f) => (t2, t1) => f(t1, t2);
    }
}
