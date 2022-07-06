using MarsonShine.Functional;
using MarsonShine.Functional.Traversable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples._13
{
    public class Program
    {
        public static void Main()
        {
            var input = Console.ReadLine();
            var result = Process(input);
            Console.WriteLine(result);
        }

        private static string Process(string? input) => input.Split(',')
            .Map(MarsonShine.Functional.String.Trim)
            .Traverse(MarsonShine.Functional.Double.Parse)
            .Map(Enumerable.Sum)
            .Match(
            () => "Errors",
            (sum) => $"Sum is {sum}"
            );
    }
}
