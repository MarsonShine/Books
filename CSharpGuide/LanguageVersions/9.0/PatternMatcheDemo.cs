using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

/// <summary>
/// 模式匹配方面的加强
/// </summary>
namespace CSharpGuide.LanguageVersions._9._0
{
    public class PatternMatcheDemo
    {
        public void Start()
        {
            // 属性模式匹配
            User user = new("marsonshine", 27);
            if(user is { Age: > 18 })
            {
                WriteLine("年级大于18岁");
            }

            string[] names = new[] { "marsonshine", "summerzhu" };
            if(names is { Length: > 2 })
            {
                WriteLine("人数大于2");
            }
            else
            {
                WriteLine("人数小于等于2");
            }

            if(user is not User userInfo)
            {
                WriteLine("非人类");
            }
            else
            {
                WriteLine("是人类");
            }
        }
    }

    public record User(string Name, int Age);
}
