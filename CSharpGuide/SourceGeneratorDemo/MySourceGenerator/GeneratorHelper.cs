using System.Collections.Generic;
using System.Text;

namespace MySourceGenerator
{
    public class GeneratorHelper
    {
        public const string Attribute = @"
namespace MySourceGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Enum)]
    public class EnumExtensionsAttribute : System.Attribute
    {
        public string ExtensionClassName { get; set; }
    }
}
";
        public static string GenerateExtensionClass(List<EnumToGenerate> enumsToGenerate)
        {
            var sb = new StringBuilder();
            sb.Append(@"
namespace MySourceGenerator
{");
            foreach (var enumToGenerate in enumsToGenerate)
            {
                sb.Append(@"
        public static partial class ").Append(enumToGenerate.ExtensionName).Append(@"
        {
            public static string ToStringFast(this ").Append(enumToGenerate.Name).Append(@" value) 
                => value switch
                {");
                foreach (var member in enumToGenerate.Values)
                {
                    sb.Append(@"
                    ")
                        .Append(enumToGenerate.Name).Append('.').Append(member)
                        .Append(" => nameof(")
                        .Append(enumToGenerate.Name).Append('.').Append(member).Append("),");
                }
                sb.Append(@"
                    _ => value.ToString(),
                };");

                sb.Append(@"
            public static string ToDescription(this ").Append(enumToGenerate.Name).Append(@" value)
                => value switch
                {");
                for (int i = 0; i < enumToGenerate.Values.Count; i++)
                {
                    string description = enumToGenerate.Descriptions[i];
                    sb.Append(@"
                    ")
                        .Append(enumToGenerate.Name).Append('.').Append(enumToGenerate.Values[i])
                        .Append(" => ")
                        .AppendFormat("\"{0}\"", description).Append(',');
                }
                sb.Append(@"
                    _ => value.ToString(),
                };
        }");
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
