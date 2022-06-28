using MarsonShine.Functional;
using MarsonShine.Functional.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples
{
    using static MarsonShine.Functional.F;
    public class Apple
    {
    }
    public class ApplePie
    {
        public ApplePie(Apple apple)
        {

        }
    }

    public class AppleStart
    {
        public static void Main()
        {
            Func<Apple, ApplePie> makePie = apple => new ApplePie(apple);

            Option<Apple> full = Some(new Apple());
            Option<Apple> empty = None;

            full.Map(makePie);  // Some(ApplePie)
            empty.Map(makePie); // None
        }
    }
}
