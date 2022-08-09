// See https://aka.ms/new-console-template for more information
using System.ComponentModel;

namespace MySourceGenerator;

partial class Program
{
    static void Main(string[] args)
    {
        HelloFrom("Generated Code");
        Console.ReadLine();

        Direction c = Direction.Down;
        Console.WriteLine(c.ToStringFast());
        Console.WriteLine(c.ToDescription());

        Color c2 = Color.Red;
        Console.WriteLine(c2.ToStringFast());
        Console.WriteLine(c2.ToDescription());
        // Console.WriteLine()
    }

    static partial void HelloFrom(string name);

    [EnumExtensions(ExtensionClassName = "DirectionExtensions")]
    public enum Direction
    {
        [Description("左")]
        Left,
        [Description("右")]
        Right,
        [Description("上")]
        Up,
        [Description("下")]
        Down,
    }
    [EnumExtensions]
    public enum Color {
        [Description("红")]
        Red,
        [Description("绿")]
        Green,
        [Description("黄")]
        Yellow,
        [Description("粉")]
        Pink,
        [Description("黑")]
        Black,
    }

}
