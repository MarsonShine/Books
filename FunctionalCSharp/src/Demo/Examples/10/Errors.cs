using Demo.Examples._10.Domain;
using MarsonShine.Functional;

namespace Demo.Examples._10
{
    public static class Errors
    {
        public static AccountNotActiveError AccountNotActive => new AccountNotActiveError();

        public static Validation<(Event, AccountState)> InsufficientBalance { get; internal set; }

        public sealed class AccountNotActiveError : Error
        {
            public override string Message { get; }
                = "The account is not active; the requested operation cannot be completed";
        }
    }
}