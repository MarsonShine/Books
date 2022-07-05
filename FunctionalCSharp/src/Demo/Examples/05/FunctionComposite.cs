using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Examples._5
{
    // 函数组合：多个函数组合成一个新函数
    // h=f*g -> h(x)=(f*g)(x) -> f(g(x))
    public class FunctionComposite
    {
        static string AbbreviateName(Person p) => Abbreviate(p.FirstName) + Abbreviate(p.LastName);
        static string AppendDomain(string localPart) => $"{localPart}@manning.com";
        static string Abbreviate(string? s) => s!.Substring(0, 2).ToLower();

        public static void Main() {
            Func<Person, string> emailFor = p => AppendDomain(AbbreviateName(p));
            var joe = new Person("Marson","Shine");
            var email = emailFor(joe);

            // v2
            var marsonShine = new Person("Marson","Shine");
            var email2 = joe.AbbreviateName().AppendDomain();

        }
    }
    // 以上方法可以改为拓展函数 -> 链式表达式能提高可读性
    public static class FunctionCompositeExtensions {
        public static string AbbreviateName(this Person p) => Abbreviate(p.FirstName) + Abbreviate(p.LastName);
        public static string AppendDomain(this string localPart) => $"{localPart}@manning.com";
        static string Abbreviate(string? s) => s!.Substring(0, 2).ToLower();
    }
}