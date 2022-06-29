using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarsonShine.Functional;
using NUnit.Framework;

namespace Demo.Exercises._7 {
    public class Answer {
        public static Func<int, int, int> Remainder = (dividend, divisor) => dividend - ((dividend / divisor) * divisor);
        [TestCase(8, -5, ExpectedResult = 3)]
        [TestCase(-8, 5, ExpectedResult = -3)]
        public static int TestRemainder(int dividend, int divisor) => Remainder(dividend, divisor);

        static Func<CountryCode, NumberType, string, PhoneNumber> CreatePhoneNumber = (contry, type, number) => new PhoneNumber(type, contry, number);

      static Func<NumberType, string, PhoneNumber> CreateUkNumber = CreatePhoneNumber.Apply((CountryCode)"uk");
      static Func<string, PhoneNumber> CreateUkMobileNumber = CreateUkNumber.Apply(NumberType.Mobile);
    }

    enum NumberType { Mobile, Home, Office }

      class CountryCode
      {
         string Value { get; }
         public CountryCode(string value) { Value = value; }
         public static implicit operator string(CountryCode c) => c.Value;
         public static implicit operator CountryCode(string s) => new CountryCode(s);
         public override string ToString() => Value;
      }

      class PhoneNumber
      {
         public NumberType Type { get; }
         public CountryCode Country { get; }
         public string Number { get; }

         public PhoneNumber(NumberType type, CountryCode country, string number)
         {
            Type = type;
            Country = country;
            Number = number;
         }
      }
}