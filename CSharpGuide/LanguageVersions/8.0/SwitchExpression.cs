using CSharpGuide.LanguageVersions._8._0.demo;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpGuide.LanguageVersions._8._0
{
    class SwitchExpression
    {
        public static RGBColor FromRainbow(Rainbow colorBand) => colorBand switch
        {
            Rainbow.Red => new RGBColor(0xFF, 0x00, 0x00),
            Rainbow.Orange => new RGBColor(0xFF, 0x7F, 0x00),
            _ => throw new ArgumentException(message: "invalid enum value,", paramName: nameof(colorBand)),
        };

        public static string GetGradeKindsof(decimal points)
        {
            switch (points)
            {
                case decimal p when (p < 60):
                    return "不及格";
                case decimal p when p >= 60 && p < 80:
                    return "一般";
                case decimal p when p >= 80 && p <= 100:
                    return "优秀";
                default:
                    return "";
            }

        }

        public static string GetGradeKindsofUsingCharpEight(decimal points) => points switch
        {
            decimal p when p < 60 => "不及格",
            decimal p when p >= 60 && p < 80 => "一般",
            decimal p when p >= 80 && p <= 100 => "优秀",
            _ => throw new ArgumentException(nameof(points)),
        };

        // 属性模式匹配
        public static decimal ComputedSalesTax(Address location, decimal salePrice) => location switch
        {
            { State: "WA" } => salePrice * 0.06M,
            { State: "MN" } => salePrice * 0.75M,
            { State: "MI" } => salePrice * 0.05M,
            _ => 0M
        };

        // 元组模式匹配
        public static string RockPaperScissors(string first, string second) => (first, second) switch
        {
            ("rock", "paper") => "rock is covered by paper. Paper wins.",
            ("rock", "scissors") => "rock breaks scissors. Rock wins.",
            (_, _) => "tie"
        };

        // 位置模式匹配
        public static Quadrant GetQuadrant(Point point) => point switch
        {
            (0, 0) => Quadrant.Origin,
            var (x, y) when x > 0 && y > 0 => Quadrant.One,
            var (x, y) when x < 0 && y > 0 => Quadrant.Two,
            var (x, y) when x < 0 && y < 0 => Quadrant.Three,
            var (x, y) when x > 0 && y < 0 => Quadrant.Four,
            var (_, _) => Quadrant.OnBorder,
            _ => Quadrant.Unknown
        };
    }

    public enum Rainbow
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Indigo,
        Violet
    }

    public enum Quadrant
    {
        Unknown,
        Origin,
        One,
        Two,
        Three,
        Four,
        OnBorder
    }

    public class Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y) => (X, Y) = (x, y);

        public void Deconstruct(out int x, out int y) => (x, y) = (X, Y);
    }
    public class RGBColor
    {
        public RGBColor(int r, int g, int b)
        {

        }
    }
}
