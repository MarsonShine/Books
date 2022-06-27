using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Demo.NonFunctionals
{
    public class MakeTransferTest
    {
        static DateTime presentDate = new DateTime(2022, 6, 21);
        static string[] validCodes = { "ABCDEFGJ123" };
        private class FakeDateTimeService : IDateTimeService {
            public DateTime UtcNow => presentDate;
        }

        [Test]
        public void WhenTransferDateIsFuture_ThenValidationPasses() {
            var transfer = new MakeTransfer { Date = new DateTime(2022, 6, 21)};
            var validator = new DateNotPastValidator();

            var actual = validator.IsValid(transfer);
            Assert.AreEqual(true, actual);
        }
        [Test]
        public void WhenTransferDateIsFuture_ThenValidationFails() {
            var sut = new DateNotPastValidatorV2(new FakeDateTimeService());
            var cmd = new MakeTransfer { Date = presentDate.AddDays(-1) };
            Assert.AreEqual(false, sut.IsValid(cmd));
        }
        // 单元测试参数化
        [TestCase(+1, ExpectedResult = true)]
        [TestCase(0, ExpectedResult = true)]
        [TestCase(-1, ExpectedResult = false)]
        public bool WhenTransferDateIsFuture_ThenValidationFailsV2(int offset) {
            var sut = new DateNotPastValidatorV2(new FakeDateTimeService());
            var cmd = new MakeTransfer { Date = presentDate.AddDays(offset) };
            return sut.IsValid(cmd);
        }

        public bool WhenBicNotFound_ThenValidationFails(string bic) => new BicExistsValidatorV3(() => validCodes)
            .IsValid(new MakeTransfer { Bic = bic});
    }
}