using MarsonShine.Functional;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples._6
{
    using static F;
    public class EitherTest
    {
        [Test]
        public void EitherTest_Success()
        {
            var r = EitherDemo.Calc(3, 0);
            Assert.AreEqual((Either<string,double>)"y cannot be 0", r);
            r = EitherDemo.Calc(-3, 3);
            Assert.AreEqual((Either<string, double>)"x / y cannot be negative", r);
            r = EitherDemo.Calc(-3, -3);
            Assert.AreEqual((Either<string, double>)1, r);
        }
    }
}
