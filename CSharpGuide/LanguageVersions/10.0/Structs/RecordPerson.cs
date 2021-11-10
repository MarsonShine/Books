using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0.Structs
{
    // 关于 record 相关介绍：https://docs.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/record
    internal record struct RecordPerson
    {
        public string FirstName { get; init; }
        public string LastName { get; init; }
    }
    public record struct Person(string FirstName, string LastName, Address Address);
    // 设置不可变的 record 结构体
    internal readonly record struct RecordPersonStruct
    {
        public string FirstName { get; init; }
        public string LastName { get; init; }
    }
}
