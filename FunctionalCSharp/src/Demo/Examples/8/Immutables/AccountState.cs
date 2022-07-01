using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples._8.Immutables
{
    public enum AccountStatus { Requested, Active, Frozen, Dormant, Closed }
    public struct Currency { }
    public class Transaction { }
    public sealed class AccountState
    {
        public Currency Currency { get; }
        public IEnumerable<Transaction> TransactionHistory { get; }
        public AccountStatus Status { get; }
        public decimal AllowedOverdraft { get; }

        public AccountState(Currency currency, AccountStatus status = AccountStatus.Requested, decimal allowedOverdraft = 0,
            IEnumerable<Transaction> transactions = null)
        {
            // ... other
            TransactionHistory = ImmutableList.CreateRange(transactions ?? Enumerable.Empty<Transaction>());
        }

        public AccountState Add(Transaction t) => new AccountState(transactions: TransactionHistory.Prepend(t),
            currency: this.Currency,
            status: this.Status,
            allowedOverdraft: this.AllowedOverdraft);

        // 设置一个可以更改属性的方法
        public AccountState With(AccountStatus? status = null, decimal? allowedOverdraft = null) => new AccountState(status: status ?? this.Status, allowedOverdraft: allowedOverdraft ?? this.AllowedOverdraft, currency: this.Currency, transactions: this.TransactionHistory);
    }

    public static class AccountStateExt
    {
        // 通过细粒度的方法使用With
        public static AccountState Freeze(this AccountState account) => account.With(status: AccountStatus.Frozen);
        public static AccountState RedFlag(this AccountState account) => account.With(AccountStatus.Frozen, 0m);
    }
}
