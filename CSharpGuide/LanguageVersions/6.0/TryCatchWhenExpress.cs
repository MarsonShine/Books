using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._6._0
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/when
    /// </summary>
    public class TryCatchWhenExpress
    {
        // 可以使用 when 带上下文关联的关键字，在两个上下文之间条件过滤
        // 在 try catch 中使用 when
        // 在 switch 中使用 when

        // 用法 1
        // 在 catch 后面跟一个 when，是说明在捕捉异常的时候当满足 when 后面的条件就会执行 catch 语句
        public static async Task<string> MakeRequest()
        {
            var client = new HttpClient();
            var streamTask = client.GetStringAsync("http://192.168.3.20:9002/asdhasd.aspx");
            try
            {
                var responseText = await streamTask;
                return responseText;
            }
            catch (HttpRequestException e) when (e.Message.Contains("301"))
            {
                return "Site Moved";
            }
            catch(HttpRequestException e) when (e.Message.Contains("404"))
            {
                return "Page Not Found";
            }
            catch (HttpRequestException e)
            {
                return e.Message;
            }
        }

        // 用法 2
        // 在 switch 后面跟一个 when，这个是在 C#7.0 才新加入的特性
        // case 标签不要求一定要互斥。when 关键字可以用来过滤指定的 case 标签的根据条件执行
        public abstract class Shape
        {
            public abstract double Area { get; }
            public abstract double Circumference { get; }
        }

        public class Rectangle : Shape
        {
            public Rectangle(double length, double width)
            {
                Length = length;
                Width = width;
            }

            public double Length { get; set; }
            public double Width { get; set; }

            public override double Area
            {
                get { return Math.Round(Length * Width, 2); }
            }

            public override double Circumference
            {
                get { return (Length + Width) * 2; }
            }
        }

        public class Square : Rectangle
        {
            public Square(double side) : base(side, side)
            {
                Side = side;
            }

            public double Side { get; set; }
        }

        private static void ShowShapeInfo(Object obj)
        {
            switch (obj)
            {
                case Shape shape when shape.Area == 0:
                    Console.WriteLine($"The shape: {shape.GetType().Name} with no dimensions");
                    break;
                case Square sq when sq.Area > 0:
                    Console.WriteLine("Information about the square:");
                    Console.WriteLine($"   Length of a side: {sq.Side}");
                    Console.WriteLine($"   Area: {sq.Area}");
                    break;
                case Rectangle r when r.Area > 0:
                    Console.WriteLine("Information about the rectangle:");
                    Console.WriteLine($"   Dimensions: {r.Length} x {r.Width}");
                    Console.WriteLine($"   Area: {r.Area}");
                    break;
                case Shape shape:
                    Console.WriteLine($"A {shape.GetType().Name} shape");
                    break;
                case null:
                    Console.WriteLine($"The {nameof(obj)} variable is uninitialized.");
                    break;
                default:
                    Console.WriteLine($"The {nameof(obj)} variable does not represent a Shape.");
                    break;
            }
        }

        public static void StartTryCatchWhen()
        {
            Console.WriteLine(MakeRequest().Result);
        }

        public static void StartSwitchCaseWhen()
        {
            Shape? sh = null;
            Shape[] shapes = { new Square(10), new Rectangle(5, 7),
                         new Rectangle(10, 10), sh!, new Square(0) };
            foreach (var shape in shapes)
                ShowShapeInfo(shape);
        }
    }
}
