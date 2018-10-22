using System;
using System.Collections.Generic;
using System.Linq;

namespace Chapter_3_Generic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(GenericCompared.CompareToDefault("x"));
            Console.WriteLine(GenericCompared.CompareToDefault(1));
            Console.WriteLine(GenericCompared.CompareToDefault(-10));
            Console.WriteLine(GenericCompared.CompareToDefault(DateTime.MinValue));

            string name = "Marson";
            string intro1 = "My name is " + name;
            string intro2 = "My name is " + name;
            Console.WriteLine(intro1 == intro2);
            Console.WriteLine(GenericCompared.AreReferenceEqual(intro1, intro2));

            AdvancedGeneric.TypeWithField<int>.field = "First";
            AdvancedGeneric.TypeWithField<string>.field = "Second";
            AdvancedGeneric.TypeWithField<DateTime>.field = "Third";

            AdvancedGeneric.TypeWithField<int>.PrintField();
            AdvancedGeneric.TypeWithField<string>.PrintField();
            AdvancedGeneric.TypeWithField<DateTime>.PrintField();

            CountingEnumerable counter = new CountingEnumerable();
            foreach (var index in counter)
            {
                Console.WriteLine(index);
            }

            ReflectorGeneric reflectorGeneric = new ReflectorGeneric();
            reflectorGeneric.Reflector();


            var list = new LinkedList_Demo(5);
            var linkeds = new LinkedList<TransportItem>(list.TransportItems);
            var deletedTransportItems = list.TransportItems.Where(t => t.Id > 1 && t.Id < 5);
            var node = linkeds.Find(deletedTransportItems.First());

            foreach (var item in deletedTransportItems)
            {
                linkeds.Remove(item);
            }
            Console.ReadLine();


        }
    }
}
