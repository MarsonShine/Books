using System;
using System.Collections.Generic;
using System.Text;

namespace Chapter_3_Generic
{
    public class ComparisonHelper<TBase, TDerived> : IComparer<TDerived>
        where TDerived : TBase
    {
        private readonly IComparer<TBase> comparer;

        public ComparisonHelper(IComparer<TBase> comparer)
        {
            this.comparer = comparer;
        }

        public int Compare(TDerived x, TDerived y)
        {
            return comparer.Compare(x, y);
        }

    }
}
