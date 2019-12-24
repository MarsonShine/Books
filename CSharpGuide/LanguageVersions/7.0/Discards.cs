using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpGuide.LanguageVersions._7._0
{
    /// <summary>
    /// 弃元：不关心这个变量值，只是一个用来存储值的未引用的临时变量。在对元素和用户定义的类型进行结构，以及使用out，ref参数调用时，起到很大的作用 用 _ 表示；使用场景：元素和类结构；out，ref参数调用；is，switch 模式匹配；想要给复制的值显示标识为弃元；
    /// </summary>
    public class Discards
    {
        public static void Start()
        {
            var (_, _, _, pop1, _, pop2) = QueryCityDataForYears("New York City", 1960, 2010);
            Console.WriteLine($"population change, 1960 to 2010: {pop2 - pop2:NO}");
        }
        private static (string, double, int, int, int, int) QueryCityDataForYears(string name, int year1, int year2)
        {
            int population1 = 0, population2 = 0;
            double area = 0;
            if (name == "New York City")
            {
                area = 468.48;
                if (year1 == 1960)
                {
                    population1 = 7781984;
                }
                if (year2 == 2010)
                {
                    population2 = 8175133;
                }
                return (name, area, year1, population1, year2, population2);
            }
            return ("", 0, 0, 0, 0, 0);
        }
    }
}
