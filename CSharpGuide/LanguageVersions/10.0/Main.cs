using CSharpGuide.LanguageVersions._10._0.StringInterpolate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0
{
    internal class Main
    {
        public static void Start(string[] args)
        {
            // 字符串插值
            var sb = new StringBuilder();
            sb.Append($"Hello {args[0]}, how are you?");
            // 字符串插值处理程序

            var logger = new Logger() { EnabledLevel = LogLevel.Warning };
            var time = DateTime.Now;

            logger.LogMessage(LogLevel.Error, $"Error Level. CurrentTime: {time}. This is an error. It will be printed.");
            logger.LogMessage(LogLevel.Trace, $"Trace Level. CurrentTime: {time}. This won't be printed.");
            logger.LogMessage(LogLevel.Warning, "Warning Level. This warning is a string, not an interpolated string expression.");

            //CallerArgumentExpression
            var a = 6;
            var b = true;
            CheckExpression(true);
            CheckExpression(b);
            CheckExpression(a > 5);
            static void CheckExpression(bool condition,[CallerArgumentExpression("condition")] string? message = null)
            {
                WriteLine($"Condition: {message}");
            }
        }
    }
}
