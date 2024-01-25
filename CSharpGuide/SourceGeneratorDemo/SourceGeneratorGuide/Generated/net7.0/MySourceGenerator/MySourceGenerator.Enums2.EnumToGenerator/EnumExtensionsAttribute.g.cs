
namespace MySourceGenerator.Enums2
{
    [System.AttributeUsage(System.AttributeTargets.Enum)]
    public class EnumExtensionsAttribute : System.Attribute
    {
        public string ExtensionClassName { get; set; }
    }
}
