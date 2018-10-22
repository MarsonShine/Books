using System;
using System.Collections.Generic;
using System.Text;

namespace Chapter_3_Generic
{
    public class ReflectorGeneric
    {
        public void Reflector()
        {
            string listTypeName = "System.Collections.Generic.List`1";
            Type defByName = Type.GetType(listTypeName);

            Type closedByName = Type.GetType(listTypeName + "[System.String]");
            Type closedMethod = defByName.MakeGenericType(typeof(string));
            Type closedByTypeof = typeof(List<string>);
            Console.WriteLine(closedMethod == closedByName);
            Console.WriteLine(closedByName == closedByTypeof);

            Type defByTypeof = typeof(List<>);
            Type defByMethod = closedMethod.GetGenericTypeDefinition();

            Console.WriteLine(defByMethod == defByName);
            Console.WriteLine(defByName == defByTypeof);
        }

        public static void PrintTypeParameter<T>()
        {
            Console.WriteLine(typeof(T));
        }


    }
}
