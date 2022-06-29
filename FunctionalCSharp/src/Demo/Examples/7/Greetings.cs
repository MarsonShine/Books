using Name = System.String;
using Greeting = System.String;
using PersonalizedGreeting = System.String;
using MarsonShine.Functional;

namespace Demo.Examples._7
{
    using static Console;
    public static class Greetings
    {
        internal static void Run()
        {
            Func<Greeting, Name, PersonalizedGreeting> greet
            = (gr, name) => $"{gr}, {name}";

            Func<Greeting, Func<Name, PersonalizedGreeting>> greetWith
            = gr => name => $"{gr}, {name}";

            var names = new Name[] { "Tristan", "Ivan" };

            WriteLine("Greet - with 'normal', multi-argument application");
            names.Map(g => greet("hello", g)).ForEach(WriteLine);

            WriteLine("Greet formally - with partial application, manual");
            var greetFormally = greetWith("Good evening");
            names.Map(greetFormally).ForEach(WriteLine);

            WriteLine("Greet informally - with partial application, general");
            var greetInformally = greet.Apply("Hey");
            names.Map(greetInformally).ForEach(WriteLine);

            PersonalizedGreeting GreeterMethod(Greeting gr, Name name) => $"{gr}, {name}";
            //Func<Name, PersonalizedGreeting> GreetWith(Greeting greeting) => GreeterMethod.Apply(greeting); // 无法通过编译

            Func<Name, PersonalizedGreeting> GreetWith_1(Greeting greeting) => FuncExt.Apply<Greeting, Name, PersonalizedGreeting>(GreeterMethod, greeting); // 只能显式指定泛型类型
            Func<Name, PersonalizedGreeting> GreetWith_2(Greeting greeting) => new Func<Name, Name, PersonalizedGreeting>(GreeterMethod).Apply(greeting); // 编写体验非常不好

            var greetWith2 = greet.Curry();
            var greetNostalgically = greetWith2("Arrivederci");
            names.Map(greetNostalgically).ForEach(WriteLine);
        }
    }
}
