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

    public class RGBColor
    {
        public RGBColor(int r, int g, int b)
        {

        }
    }
}
