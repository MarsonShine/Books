// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

// 无参构造函数
var order = new Order() { Name = "" };

// 无参构造函数报错
var requiredOrder = new RequiredOrder(){};

// required 修饰的类，哪怕是无参构造函数， new() 的约束无效
// 具体详见：https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/required 中的第六点已讲到
// 下面就无法正常编译
// GenericExtensions.Include<RequiredOrder>(requiredOrder);
// 但是 List<T> 可以
GenericExtensions.Include<List<RequiredOrder>>(null!);



public class Order
{
    public Order() { }
    public Order(string name, string address) => (Name, Address) = (name, address);
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class RequiredOrder
{
    public RequiredOrder() { }
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public RequiredOrder(string name, string address) => (Name, Address) = (name, address);
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
}

public static class GenericExtensions
{
    public static T Include<T>(T source) where T : class, new()
    {
        return source ?? new T();
    }
}