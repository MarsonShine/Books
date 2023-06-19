using System;

namespace MySourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GeneratorSerializableAttribute : Attribute
    {
    }
}
