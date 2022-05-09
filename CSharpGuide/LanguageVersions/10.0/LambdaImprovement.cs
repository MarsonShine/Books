using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.LanguageVersions._10._0
{
    internal class LambdaImprovement
    {
        public static void Start()
        {
            Func<string,int> parse = (string s) => int.Parse(s);
            // C#9.0无法做到委托Lambda的类型推断
            var parse2 = (string s) => int.Parse(s);
#if ERROR
            // 以下表达式无法推断出明确的类型信息
            var parse = s => int.Parse(s);
#endif
            object parse3 = (string s) => int.Parse(s);   // Func<string, int>
            Delegate parse4 = (string s) => int.Parse(s); // Func<string, int>

            LambdaExpression parseExpr = (string s) => int.Parse(s); // Expression<Func<string, int>>
            Expression parseExpr2 = (string s) => int.Parse(s);       // Expression<Func<string, int>>

            //var choose = (bool b) => b ? 1 : "two"; // ERROR: Can't infer return type
            // C#10可以显式申明返回类型
            var choose2 = object (bool b) => b ? -1 : "one";

            // lambda属性
            //Func<string, int> parse5 =[Example(1)](s) => int.Parse(s);
        }
    }
}
