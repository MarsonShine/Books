using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter13_HighPerformancePassByReference
{
    /*
    Span<T> 和 内存分配
     */
    public class SpanAndMemoryAccess
    {
        unsafe public static void Start()
        {
            string alphabet = "abcdefghijklmnopqrstuvwxyz";
            Random random = new Random();
            Console.WriteLine(Generate(alphabet, random, 10));
            Console.WriteLine(Generate2(alphabet, random, 10));
            Console.WriteLine(Generate3(alphabet, random, 10));
            Console.WriteLine(Generate4(alphabet, random, 10));

            Span<int> span = stackalloc int[] { 1, 2, 3 };
            int* pointer = stackalloc int[] { 4, 5, 6 };
        }
        // 传统方法：这里有两初进行了内存分配：一个是 chars 数组，一个是 return new string()
        // 数据需要从 chars 复制到新的对象以便构造 string
        public static string Generate(string alphabet, Random random, int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[random.Next(alphabet.Length)];
            }
            return new string(chars);
        }
        // 用不安全代码改进上面的实现
        // 下面的执行只发生了一次堆分配：就是 string()
        unsafe static string Generate2(string alphabet, Random random, int length)
        {
            char* chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[random.Next(alphabet.Length)];
            }
            return new string(chars);
        }
        // 通过 Span + stackalloc 安全代码来实现不安全代码同样的效果
        public static string Generate3(string alphabet, Random random, int length)
        {
            Span<char> chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = alphabet[random.Next(alphabet.Length)];
            }
            return new string(chars);
        }

        delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

        public static string Generate4(string alphabet, Random random, int length) =>
        string.Create(length, (alphabet, random), (span, state) =>
        {
            var alphabet2 = state.alphabet;
            var random2 = state.random;
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = alphabet2[random2.Next(alphabet2.Length)];
            }
        });
    }
}
