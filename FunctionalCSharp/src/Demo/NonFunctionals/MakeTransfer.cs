using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Demo.NonFunctionals
{
    public abstract class Command { }
    public sealed class MakeTransfer:Command
    {
        public Guid DebitedAccountId { get; set; }
        public string Beneficiary { get; set; }
        public string Iban { get; set; }
        public string Bic { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public interface IValidator<T> {
        bool IsValid(T t);
    }

    public sealed class BicExistsValidator:IValidator<MakeTransfer> {
        static readonly Regex regex = new Regex("^[A-Z]{6}[A-Z1-9]{5}$");
        // 如果是带有业务状态的验证呢？如Bic卡号可能先要从持久库中获取，然后在判断卡号是否有效
        public bool IsValid(MakeTransfer cmd) => regex.IsMatch(cmd.Bic);
    }
    public sealed class DateNotPastValidator : IValidator<MakeTransfer>
    {
        // DateTime.UtcNow 就是状态，因此该方法是非纯函数的
        public bool IsValid(MakeTransfer cmd) => DateTime.UtcNow.Date <= cmd.Date.Date;
    }

    public interface IDateTimeService {
        DateTime UtcNow { get; } // 将不纯的行为封装在另一个对象中
    }
    public class DefaultDateTimeService : IDateTimeService {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public sealed class DateNotPastValidatorV2 : IValidator<MakeTransfer>
    {
        private readonly IDateTimeService clock;
        public DateNotPastValidatorV2(IDateTimeService clock) {
            this.clock = clock;
        }
        // DateTime.UtcNow 就是状态，因此该方法是非纯函数的
        public bool IsValid(MakeTransfer cmd) => clock.UtcNow.Date <= cmd.Date.Date; // 是否纯函数取决于IDateTimeService接口
    }

    // 这种方式先要从外部加载validCodes再注入进来
    public sealed class BicExistsValidatorV2:IValidator<MakeTransfer> {
        readonly IEnumerable<string> validCodes;
        // 如果是带有业务状态的验证呢？如Bic卡号可能先要从持久库中获取，然后在判断卡号是否有效
        public bool IsValid(MakeTransfer cmd) => validCodes.Contains(cmd.Bic);
    }
    // 这种方式对单元测试也很友好
    public sealed class BicExistsValidatorV3: IValidator<MakeTransfer> {
        readonly Func<IEnumerable<string>> getValidCodes;

        public BicExistsValidatorV3(Func<IEnumerable<string>> getValidCodes) {
            this.getValidCodes = getValidCodes;
        }

        public bool IsValid(MakeTransfer cmd) => getValidCodes().Contains(cmd.Bic);
    }
}