using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace MySourceGenerator.Enums
{
    public readonly record struct EnumToGenerate
    {
        public readonly string Name;
        public readonly EquatableArray<string> Values;

        public EnumToGenerate(string name,List<string> values)
        {
            Name = name;
            Values = new([.. values]);
        }
    }
}
