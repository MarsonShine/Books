using System;
using System.Diagnostics.CodeAnalysis;
/// <summary>
/// 参考链接 https://devblogs.microsoft.com/dotnet/announcing-net-5-0-rc-1/
/// </summary>
namespace CSharpGuide.LanguageVersions._9._0 {
    public class RecordIdentifierDemo {
        // 初始化
        public void Initial() {
            // 方式1
            var person = new Person();
            // 方式二
            Person p = new();
            string firstName = "marson";
            string lastName = "shine";

            var ps = new Person {
                FirstName = firstName,
                LastName = lastName
            };
            var ps2 = new Person {
                FirstName = firstName,
                LastName = lastName
            };

            // record 类型，常用语只读场景，表示不可变类型
            ImmutablePerson ip = new(firstName, lastName);
            ImmutablePerson ip2 = new(firstName, lastName);
            Console.WriteLine($"FullName = {ip.FirstName} {ip.LastName}");
            Console.WriteLine($"record 类型比较 ip == ip2 : {ip == ip2}");
            Console.WriteLine($"record 类型比较 ip.HashCode() = {ip.GetHashCode()}; ip2.HashCode() = {ip2.GetHashCode()}; 相等性 : {ip.GetHashCode() == ip2.GetHashCode()}");
            ImmutablePerson ip3 = ip2;
            ip3.FirstName = "summer";
            Console.WriteLine($"ip2.FirstName = {ip2.FirstName} ip3.FirstName = {ip3.FirstName}");
            Console.WriteLine($"record 类型比较 ip.HashCode() = {ip.GetHashCode()}; ip2.HashCode() = {ip2.GetHashCode()}; 相等性 : {ip.GetHashCode() == ip2.GetHashCode()}");
            // class 类型比较
            Person p1 = new() { FirstName = "marsonshine", LastName = "shine" };
            Person p2 = new() { FirstName = "marsonshine", LastName = "shine" };
            Console.WriteLine($"class 类型比较 p1 == p2 : {p1 == p2}");
            Console.WriteLine($"class 类型比较 ps.HashCode() = {ps.GetHashCode()}; ps2.HashCode() = {ps2.GetHashCode()}; 相等性 : {ps.GetHashCode() == ps2.GetHashCode()}");

            // record 上的 with 关键字
            // 可以指定属性必须和选填，在构造函数参数体现是必填
            // 选填是跟以前属性写法一样
            // 如果你想在原来的类型下对增加一些属性值，或者更改某些属性值，但是又不想破坏 record 带来的数据不变性，这个时候我们就可以使用 with 关键字，它会从已有的 record 类型按值复制出来一个新的 record 类型
            LoginResource login = new("MarsonShine", "123$%^") { RememberMe = true };
            LoginResource loginLowercased = login with { UserName = login.UserName.ToLowerInvariant() };
            Console.WriteLine(login);
            Console.WriteLine(loginLowercased);

            var weight = 200;
            WeightMeasurement measurement = new(DateTime.Now, weight) {
                Pounds = WeightMeasurement.GetPounds(weight)
            };
        }
    }

    public class Person {
        public string? FirstName { get; init; }
        public string? LastName { get; set; }
    }

    public record ImmutablePerson {
        public ImmutablePerson(string firstName, string lastName) {
            this.FirstName = firstName;
            this.LastName = lastName;
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    // record 简写方式
    public record ImmutablePerson2(string FirstName, string LastName);
}
// 可以指定属性必须和选填，在构造函数参数体现是必填
// 选填是跟以前属性写法一样
// 如果你想在原来的类型下对增加一些属性值，或者更改某些属性值，但是又不想破坏 record 带来的数据不变性，这个时候我们就可以使用 with 关键字，它会从已有的 record 类型按值复制出来一个新的 record 类型
public record LoginResource(string UserName, string Password) {
    public bool RememberMe { get; init; }
}

// 如果你想要使 record 类的其中某个属性变为必填，那么你就必须要在构造函数中添加这个属性参数
// 试想一下，如果这个类弥漫程度很高，那么势必要花费很长的精力在创建处一个个改
// 这个时候就可以用 record 继承来变成另一个 record 类来使这个属性变为必填项，这样就可以在新的需求中转而使用这个新的类型
// 以上面的 LoginResource 为例，增加一个属性 LastLoggedIn 为必填项
public record LoginWithUserDataResource(string UserName, string Password, DateTime LastLoggedIn) : LoginResource(UserName, Password) {
    public int Discounter { get; init; }
    public bool FreeShipping { get; init; }
}
// 上面这个例子就拓展了 LoginResource 类成了新类，并将属性 LastLoggedIn 必填，Discounter，FreeShipping 为选填

// 初始化就可以借助 GetPounds 来讲 kg 转换为磅
public record WeightMeasurement(DateTime date, double Kilograms) {
    public double Pounds { get; init; }
    public static double GetPounds(double kilograms) => kilograms * 2.20462262;
}