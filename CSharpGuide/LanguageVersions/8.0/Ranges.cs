using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpGuide.LanguageVersions._8._0
{
    /// <summary>
    /// <see cref="https://docs.microsoft.com/zh-cn/dotnet/csharp/tutorials/ranges-indexes"/>
    /// ^ 运算符，指定一个索引与序列末尾相关。
    /// System.Index 表示一个序列索引。
    /// System.Range 表示序列的子范围。
    /// 范围运算符 (..)，用于指定范围的开始和末尾，就像操作数一样。
    /// [0..^0] 表示整个范围 等价于 [0..array.Length]
    /// </summary>
    class Ranges
    {
        int[] array = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public void Range()
        {
            foreach (var item in Enumerable.Range(1,100))
            {
                Console.WriteLine(item);
            }
        }

        public void RangeUsingCSharpEight()
        {
            Range r = 1..100;   //表示元素从array[1] - array[99];注意 array[100] 不在范围内
            foreach (var item in array[r])
            {
                Console.WriteLine(item);
            }
        }

        public int GetLastIndex()
        {
            Console.WriteLine($"最后一个元素是 {array[^1]}");
            return array[^1];
        }

        public void Start()
        {
            var lazyDog = array[^2..^0];
            foreach (var word in lazyDog)
                Console.Write($"< {word} >");

            var all = array[..];
            var firstPhrase = array[..4];   //0开始 长度为4
            var lastPhrase = array[6..]; //从下标6开始，长度到结束
            Console.WriteLine($"array[..] = ");
            foreach (var item in all)
            {
                Console.Write($"< {item} >");
            }
            Console.WriteLine($"array[..4] = ");
            foreach (var item in firstPhrase)
            {
                Console.Write($"< {item} >");
            }
            Console.WriteLine($"array[6..] = ");
            foreach (var item in lastPhrase)
            {
                Console.Write($"< {item} >");
            }
        }
    }
}
