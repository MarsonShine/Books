using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpGuide.is_operator
{
    internal class IsOperatorInCsharp
    {
        public static bool IsFirstFridayOfOctober(DateTime date) => date is { Month:5, Day: <= 7, DayOfWeek: DayOfWeek.Friday };

        public static bool Pattern()
        {
            int[] empty = { };
            int[] one = { 1 };
            int[] odd = { 1, 3, 5 };
            int[] even = { 2, 4, 6 };
            int[] fib = { 1, 1, 2, 3, 5 };

            WriteLine(odd is [1, _, 2, ..]);   // false
            WriteLine(fib is [1, _, 2, ..]);   // true
            WriteLine(fib is [_, 1, 2, 3, ..]);     // true
            WriteLine(fib is [.., 1, 2, 3, _]);     // true
            WriteLine(even is [2, _, 6]);     // true
            WriteLine(even is [2, .., 6]);    // true
            WriteLine(odd is [.., 3, 5]); // true
            WriteLine(even is [.., 3, 5]); // false
            WriteLine(fib is [.., 3, 5]); // true
        }
    }
}
