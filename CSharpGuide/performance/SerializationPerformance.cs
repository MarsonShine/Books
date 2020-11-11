using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CSharpGuide.performance
{
    public class SerializationPerformance
    {
        // TODO
    }

    public class RequestBody
    {
        [JsonConstructor]
        public RequestBody(string name,int age,Class @class)
        {
            Name = name;
            Age = age;
            Class = @class;
        }
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Description { get; set; }
        public Class? Class { get; set; }
    }

    public class Class
    {
        public int ClassId { get; set; }
        [AllowNull]
        public string ClassName { get; set; }
        public int SchoolId { get; set; }
        [AllowNull]
        public string SchoolName { get; set; }
    }
}
