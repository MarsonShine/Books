using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CSharpGuide.LanguageVersions._7._0
{
    public class ValueTuples
    {
        public static ValueTuple<T> Create<T>(T value)
        {
            return ValueTuple.Create<T>(value);
        }

        public static ValueTuple<T,T> Create<T>(T value,T value2)
        {
            return ValueTuple.Create<T, T>(value, value2);
        }

        public static Tuple<T> CreateTuple<T>(T value) {
            return Tuple.Create(value);
        }
    }


    //public class CustomValueTuple<T1> : ITuple
    //{
    //    public T1 Item1;
    //    public object? this[int? index] {
    //        get {
    //            if (index != 0) throw new IndexOutOfRangeException();
    //            return Item1;
    //        }
    //    }

    //    public int? Length => 1;
    //}
}
