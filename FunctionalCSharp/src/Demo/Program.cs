// See https://aka.ms/new-console-template for more information
global using System; 
using Demo.Magics;
using Demo.NonFunctionals;
using static System.Console;
using static MarsonShine.Functional.F;

Console.WriteLine("Hello, World!");

// 通过给委托定义拓展函数
Func<int, int, int> divide = (x, y) => x / y;
divide(10, 2);

var divideBy = divide.SwapArgs();
divideBy(2, 10);

// 带有状态的非纯函数是不利于并行化的
var list = new List<string>{"coffee beans", "BANANAS", "Dates" };
new ListFormatter()
    .Format(list)
    .ForEach(WriteLine);
// 并行
// list.Select(StringExt.ToSentenceCase).ToList();
// list.AsParallel().Select(StringExt.ToSentenceCase).ToList(); // 因为ToSentenceCase是纯函数，无状态的，所以可以安全的并行
// 但是非纯函数就不行了
new ListFormatter()
    .ParellerFormat(list)
    .ForEach(WriteLine);

// 函数式编程重构
// 通过范围/分区
Enumerable.Zip(
    new[] {1, 2, 3},
    new[] {"ichi", "ni", "san"}, (number, name) => $"In Japanese, {number} is:{name}");

Demo.Functionals.ListFormatter.ParallelFormat(list)
    .ForEach(WriteLine);

var firstName = Some("marsonshine");
var middleName = None;
