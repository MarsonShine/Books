using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0.Structs
{
    internal class Program
    {
        public static void Start()
        {
            RecordPerson person = new() { FirstName = "Marson", LastName = "Shine" };
            var person2 = person with { LastName = "Zhu" };

            // 拓展属性模式
            object obj = new Person
            {
                FirstName = "Marson",
                LastName = "Shine",
                Address = new Address { City = "Seattle" }
            };

            if (obj is Person { Address: { City:"Seattle"} })
            {
                WriteLine("Seattle");
            }

            if (obj is Person { Address.City:"Seattle"})    // C#10 拓展属性模式
            {
                WriteLine("Seattle");
            }
        }
    }
}
