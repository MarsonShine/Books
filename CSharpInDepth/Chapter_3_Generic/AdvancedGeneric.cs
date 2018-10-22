using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Chapter_3_Generic
{
    public class AdvancedGeneric
    {
        internal class TypeWithField<T>
        {
            public static string field;
            public static void PrintField()
            {
                Console.WriteLine(field + ":" + typeof(T).Name);
            }
        }
    }

    public class Outer<T>
    {
        public class Inner<U, V>
        {
            static Inner()
            {
                Console.WriteLine("Outer<{0}>.Inner<{1},{2}>", typeof(T).Name, typeof(U).Name, typeof(V).Name);
            }
            public static void DummyMethod() { }
        }
    }

    #region 3.4.3 泛型迭代
    public class CountingEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            return new CountingEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public class CountingEnumerator : IEnumerator<int>
    {
        int current = -1;
        public int Current => current;

        object IEnumerator.Current => throw new NotImplementedException();

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            current++;
            return current < 10;
        }
        
        public void Reset()
        {
            current = -1;
        }
    }
    #endregion
}
