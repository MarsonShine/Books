using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0.StringInterpolate
{
    [InterpolatedStringHandler]
    public ref struct LogInterpolatedStringHandler
    {
        StringBuilder builder;

        public LogInterpolatedStringHandler(int literalLength,int formmatedCount)
        {
            builder = new StringBuilder(literalLength);
            WriteLine($"\tliteral length:{literalLength}, formmatedCount: {formmatedCount}");
        }

        public void AppendLiteral(string s)
        {
            WriteLine($"\tAppendLiteral called: ({s})");
            builder.Append(s);
            WriteLine($"\tAppendLiteral the literal string");
        }

        public void AppendFormatted<T>(T t)
        {
            WriteLine($"\tAppendFormatted called: {{{t}}} is of type {typeof(T)}");

            builder.Append(t?.ToString());
            WriteLine($"\tAppended the formatted object");
        }

        internal string GetFormattedText() => builder.ToString();
    }
}
