using System.Collections.Generic;
using System.Text;

namespace MySourceGenerator.Enums2;
public static class SourceGenerationHelper
{
    public const string Attribute = @"
namespace MySourceGenerator.Enums
{
    [System.AttributeUsage(System.AttributeTargets.Enum)]
    public class EnumExtensionsAttribute : System.Attribute
    {

    }
}
";

    public static string GenerateExtensionClass(List<EnumToGenerate> enumToGenerates)
    {
        var sb = new StringBuilder();
        sb.Append(@"
namespace MySourceGenerator.Enums
{");
        foreach (var enumToGenerate in enumToGenerates)
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
                    ").Append(enumToGenerate.Name).Append('.').Append(member)
                    .Append(" => nameof(")
                    .Append(enumToGenerate.Name).Append('.').Append(member).Append("),");
            }
            sb.Append(@"
                    _ => value.ToString(),
                };
        }
    ");
        }
        sb.Append('}');
        return sb.ToString();
    }
}
