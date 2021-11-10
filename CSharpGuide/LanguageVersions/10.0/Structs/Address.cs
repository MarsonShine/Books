using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0.Structs
{
    public struct Address
    {
        public Address()
        {
            City = "unknown";
        }
        public Address(string city)
        {
            City = city;
        }
        public string City { get; init; }
    }
}
