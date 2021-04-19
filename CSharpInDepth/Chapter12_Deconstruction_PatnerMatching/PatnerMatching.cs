using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter12_Deconstruction_PatnerMatching
{
    /*
     C#7.0 的模式匹配有三种
    1：常熟匹配：当输入 input 与目标“相等”时进入匹配逻辑；这里的相等是通过 == 操作符实、Object.Equals 方法
    2：类型匹配
    3：var 匹配: someExpression is var x，这种模式匹配总是成功的，哪怕目标对象为 null，所以这就需要我们在编写代码的时候要判断 x 具体是什么值。（因为var 匹配总会成功匹配目标类型的值/默认值）
    这经常用于最后的匹配错误场景
    还有模式匹配的谓语从句的用法：case pattern when expression:
     */
    public class PatnerMatching
    {
        // 一般方法
        public static void Match(object input)
        {
            if (input is "hello")
                Console.WriteLine("Input is string hello");
            else if (input is 5L)
                Console.WriteLine("Input is long 5");
            else if (input is 10)
                Console.WriteLine("Input is int 10");
            else
                Console.WriteLine("Input didn't match hello");
        }
        // 常熟模式匹配
        public static void ConstPatnerMatch(object input)
        {
            switch (input)
            {
                case "hello":
                    Console.WriteLine("Input is string hello");
                    break;
                case 5L:
                    Console.WriteLine("Input is long 5");
                    break;
                case 10:
                    Console.WriteLine("Input is int 10");
                    break;
                default:
                    Console.WriteLine("Input didn't match hello");
                    break;
            }
        }

        // 可空类型的模式匹配
        public static void CheckType<T>(object value)
        {
            if (value is T t)
            {
                Console.WriteLine($"{t} 是类型 {typeof(T)}");
            }
            else
            {
                Console.WriteLine($"{value ?? "null"} 不是类型 {typeof(T)}");
            }
        }

        // 泛型的模式匹配
        public static void DisplayShapes<T>(List<T> shapes) where T : Shape
        {
            foreach (var shape in shapes)
            {
                switch (shape)
                {
                    case Circle c:
                        Console.WriteLine($"圆半径为 {c.Redius}");
                        break;
                    case Rectangle r:
                        Console.WriteLine($"长方形长 {r.Width} 高为 {r.Height}");
                        break;
                    case Triangle t:
                        Console.WriteLine($"三角形边分别为 {t.SideA} {t.SideB} {t.SideC}");
                        break;
                    case var actualShape: // var 模式匹配
                        Console.WriteLine($"匹配失败，目标实际类型为 {actualShape}");
                        break;
                }
            }
        }
        // 模式匹配的谓语从句(带条件匹配)用法
        public static int Fib(int n)
        {
            switch (n)
            {
                case 0:return 0;
                case 1:return 1;
                case var _ when n > 1:return Fib(n - 2) + Fib(n - 1);
                default:
                    throw new ArgumentOutOfRangeException(nameof(n), "input 必须是非负数");
            }
        }

        public abstract class Shape
        {
            public double Area { get; set; }
            public static double Perimeter(Shape shape)
            {
                if (shape == null)
                    throw new ArgumentNullException(nameof(shape));

                Rectangle rectangle = shape as Rectangle;
                if (rectangle != null)
                    return 2 * (rectangle.Height + rectangle.Width);
                Circle circle = shape as Circle;
                if (circle != null)
                    return 2 * Math.PI * circle.Redius;
                Triangle triangle = shape as Triangle;
                if (triangle != null)
                    return triangle.SideA + triangle.SideB + triangle.SideC;
                throw new ArgumentException("类型匹配错误");
            }
            // 类型匹配，变量匹配
            static double Perimater_TypeMatch(Shape shape)
            {
                if (shape == null)
                    throw new ArgumentNullException(nameof(shape));
                if (shape is Rectangle rectangle)   // 是将 as + if 组合用模式匹配替换了
                    return 2 * (rectangle.Height + rectangle.Width);
                if (shape is Circle circle)
                    return 2 * Math.PI * circle.Redius;
                if (shape is Triangle triangle)
                    return triangle.SideA + triangle.SideB + triangle.SideC;
                throw new ArgumentException("类型匹配错误");
            }
        }

        public class Rectangle : Shape
        {
            public double Height { get; internal set; }
            public double Width { get; internal set; }
        }
        public class Circle : Shape
        {
            public double Redius { get; internal set; }
        }
        public class Triangle : Shape
        {
            public double SideC { get; internal set; }
            public double SideB { get; internal set; }
            public double SideA { get; internal set; }
        }
    }
}
