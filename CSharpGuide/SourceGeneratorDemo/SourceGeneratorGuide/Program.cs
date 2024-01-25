// See https://aka.ms/new-console-template for more information
using MySourceGenerator.Enums2;
using MySourceGenerator.NestedEnum;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MySourceGenerator;

partial class Program
{
    static void Main(string[] args)
    {
        HelloFrom("Generated Code");
        // Console.ReadLine();

        Direction c = Direction.Down;
        Console.WriteLine(c.ToStringFast());
        //Console.WriteLine(c.ToDescription());

        //_ = Color.Red.ToStringFast();

        //Color c2 = Color.Red;
        //Console.WriteLine(c2.ToStringFast());
        //Console.WriteLine(c2.ToDescription());
        // Console.WriteLine()
        GeneratedClass.GeneratedMethod();
        new UserClass().UserMethod();

        RoleEnum.Admin.ToStringFast();
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
    [Enums2.EnumExtensions]
    public enum Color
    {
        [Display(Name = "红")]
        Red,
        [Display(Name = "绿")]
        Green,
        [Display(Name = "黄")]
        Yellow,
        [Display(Name = "粉")]
        Pink,
        [Display(Name = "黑")]
        Black,
    }
    [GeneratorSerializable]
    partial class MyRecord
    {
        public string ITem1 { get; }
        public int ITem2 { get; }
    }
}
