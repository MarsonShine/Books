using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.NonFunctionals
{
    public static class StringExt
    {
        // 纯函数，只跟输入输出有关系
        public static string ToSentenceCase(this string s) => s.ToUpper()[0] + s.ToLower().Substring(1);
    }
    // 非纯函数，带有counter状态
    class ListFormatter {
        int counter;
        string PrependCounter(string s) => $"{++counter}. {s}";
        public List<string> Format(List<string> list) => 
            list.Select(StringExt.ToSentenceCase)
            .Select(PrependCounter)
            .ToList();
        // wrong，非纯函数无法安全的并行
        public List<string> ParellerFormat(List<string> list) => 
            list.AsParallel()
            .Select(StringExt.ToSentenceCase)
            .Select(PrependCounter)
            .ToList();
    }
}