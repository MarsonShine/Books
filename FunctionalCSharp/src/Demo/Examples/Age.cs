using System;
using System.Diagnostics.CodeAnalysis;
using MarsonShine.Functional;

namespace Demo.Examples
{
    using static F;
    public struct Age
    {
        private int Value { get; }
        private Age(int value)
        {
            if (!IsValid(value))
                throw new ArgumentException($"{value} is not a valid age");

            Value = value;
        }

        private static bool IsValid(int age) => 0 <= age && age < 120;

        public static Option<Age> Of(int age) => IsValid(age) ? Some(new Age(age)) : None;

        public static bool operator <(Age l, Age r) => l.Value < r.Value;
        public static bool operator >(Age l, Age r) => l.Value > r.Value;

        public static bool operator <(Age l, int r) => l < new Age(r);
        public static bool operator >(Age l, int r) => l > new Age(r);

        public override string ToString() => Value.ToString();

        enum Risk
        {
            Low,
            Medium,
            High,
        }

        Risk CalculateRiskProfile(Age age) => (age.Value < 60) ? Risk.Low : Risk.Medium;
    }

    public class AgeInvoke
    {
        public static void Main()
        {
            Func<string, Option<Age>> parseAge = s => Int.Parse(s).Bind(Age.Of);
            //Func<string, Option<Age>> parseAge2 = s => Int.Parse(s).Map(age => new Age(age));

            var optionalAges = Polulation.Map(p => p.Age);
            var stateAges = Polulation.Bind(p => p.Age);
        }

        static IEnumerable<Subject> Polulation => new[]
        {
            new Subject{Age = Age.Of(33)},
            new Subject{},
            new Subject{Age = Age.Of(37)}
        };
    }

    public static class AskForValidAgeAndPrintFlatteringMessage
    {
        public static void Main() => WriteLine($"Only {ReadAge()}! That's young!");
        static Age ReadAge() => ParseAge(Promt("Please enter your age"))
            .Match(
            () => ReadAge(), // 如果无效，就递归ReadAge，有点意思
            (age) => age
            );

        static Option<Age> ParseAge(string? s) => Int.Parse(s!).Bind(Age.Of);

        static string? Promt(string promt)
        {
            WriteLine(promt);
            return ReadLine();
        }
    }

    class Subject
    {
        public Option<Age> Age { get; set; }
    }
}
