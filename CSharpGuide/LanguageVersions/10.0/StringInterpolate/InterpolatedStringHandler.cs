using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0.StringInterpolate
{
    // https://docs.microsoft.com/zh-cn/dotnet/csharp/whats-new/tutorials/interpolated-string-handler
    // 实现自定义字符串插值处理程序
    public enum LogLevel
    {
        Off,
        Critical,
        Error,
        Warning,
        Information,
        Trace
    }

    public class Logger
    {
        public LogLevel EnabledLevel { get; init; } = LogLevel.Error;
        public void LogMessage(LogLevel level, string msg)
        {
            if (EnabledLevel < level) return;
            WriteLine(msg);
        }

        public void LogMessage(LogLevel level,LogInterpolatedStringHandler builder)
        {
            if (EnabledLevel < level) return;
            WriteLine(builder.GetFormattedText());
        }
    }
}
