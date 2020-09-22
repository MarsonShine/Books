using System;
using System.Diagnostics.CodeAnalysis;

namespace CSharpGuide.LanguageVersions._9._0 {
    public class RecordIdentifierDemo {
        // 初始化
        public void Initial() {
            // 方式1
            var person = new Person();
            // 方式二
            Person p = new();

            var ps = new Person {
                FirstName = "marson",
                LastName = "shine"
            };

            // record 类型，常用语只读场景，表示不可变类型
            string firstName = "marson";
            string lastName = "shine";
            ImmutablePerson ip = new(firstName, lastName);
            ImmutablePerson ip2 = new(firstName, lastName);
            Console.WriteLine($"FullName = {ip.FirstName} {ip.LastName}");
            Console.WriteLine($"record 类型比较 ip == ip2 : {ip == ip2}");

            ImmutablePerson ip3 = ip2;
            ip3.FirstName = "summer";
            Console.WriteLine($"ip2.FirstName = {ip2.FirstName} ip3.FirstName = {ip3.FirstName}");
            // class 类型比较
            Person p1 = new() { FirstName = "marsonshine", LastName = "shine" };
            Person p2 = new() { FirstName = "marsonshine", LastName = "shine" };
            Console.WriteLine($"class 类型比较 p1 == p2 : {p1 == p2}");
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