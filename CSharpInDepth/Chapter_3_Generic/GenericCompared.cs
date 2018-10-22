using System;
using System.Collections.Generic;
using System.Text;

namespace Chapter_3_Generic
{
    public class GenericCompared
    {
        public static int CompareToDefault<T>(T value)
            where T : IComparable<T>
        {
            return value.CompareTo(default(T));
        }

        public static bool AreReferenceEqual<T>(T first, T second)
            where T : class
        {
            return first == second;
        }
    }
}
