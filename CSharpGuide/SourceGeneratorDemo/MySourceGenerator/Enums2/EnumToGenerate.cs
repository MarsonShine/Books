using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace MySourceGenerator.Enums2
{
    public readonly record struct EnumToGenerate
    {
        public readonly string Name;
        public readonly string ExtensionName;
        public readonly EquatableArray<string> Values;

        public EnumToGenerate(string extensionName, string name, List<string> values)
        {
            ExtensionName = extensionName;
            Name = name;
            Values = new([.. values]);
        }
    }
}
