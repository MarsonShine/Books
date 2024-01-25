
namespace MySourceGenerator.Enums2
{
    public static partial class DirectionExtensions
    {
        public static string ToStringFast(this MySourceGenerator.Program.Direction value)
            => value switch
            {
                    MySourceGenerator.Program.Direction.Left => nameof(MySourceGenerator.Program.Direction.Left),
                    MySourceGenerator.Program.Direction.Right => nameof(MySourceGenerator.Program.Direction.Right),
                    MySourceGenerator.Program.Direction.Up => nameof(MySourceGenerator.Program.Direction.Up),
                    MySourceGenerator.Program.Direction.Down => nameof(MySourceGenerator.Program.Direction.Down),
                    _ => value.ToString(),
            };
        }
    
    public static partial class EnumExtensions
    {
        public static string ToStringFast(this MySourceGenerator.Program.Color value)
            => value switch
            {
                    MySourceGenerator.Program.Color.Red => nameof(MySourceGenerator.Program.Color.Red),
                    MySourceGenerator.Program.Color.Green => nameof(MySourceGenerator.Program.Color.Green),
                    MySourceGenerator.Program.Color.Yellow => nameof(MySourceGenerator.Program.Color.Yellow),
                    MySourceGenerator.Program.Color.Pink => nameof(MySourceGenerator.Program.Color.Pink),
                    MySourceGenerator.Program.Color.Black => nameof(MySourceGenerator.Program.Color.Black),
                    _ => value.ToString(),
            };
        }
    
    public static partial class EnumExtensions
    {
        public static string ToStringFast(this MySourceGenerator.NestedEnum.RoleEnum value)
            => value switch
            {
                    MySourceGenerator.NestedEnum.RoleEnum.None => nameof(MySourceGenerator.NestedEnum.RoleEnum.None),
                    MySourceGenerator.NestedEnum.RoleEnum.Admin => nameof(MySourceGenerator.NestedEnum.RoleEnum.Admin),
                    _ => value.ToString(),
            };
        }
    }