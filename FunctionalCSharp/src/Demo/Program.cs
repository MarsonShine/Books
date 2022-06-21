// See https://aka.ms/new-console-template for more information
using Demo.Magics;

Console.WriteLine("Hello, World!");

// 通过给委托定义拓展函数
Func<int, int, int> divide = (x, y) => x / y;
divide(10, 2);

var divideBy = divide.SwapArgs();
divideBy(2, 10);

// 调用

