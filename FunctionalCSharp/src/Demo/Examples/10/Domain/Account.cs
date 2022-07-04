using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarsonShine.Functional;

namespace Demo.Examples._10.Domain
{
    using static F;
    public static class Account
    {
        public static AccountState Create(CreatedAccount evt) => new AccountState(evt.Currency, AccountStatus.Active);

        public static AccountState Apply(this AccountState account, Event evt) => evt switch {
            DepositedCash e => account.Credit(e.Amount),
            DebitedTransfer e => account.Debit(e.DebitedAmount),
            FrozeAccount e => account.WithStatus(AccountStatus.Frozen),
            _ => throw new ArgumentException(nameof(evt));
        };

        public static Option<AccountState> From(IEnumerable<Event> history) => history.Match(
            Empty: () => None,
            Otherwise: (createdEvent, otherEvents) => Some(
                otherEvents.Aggregate(
                    seed: Account.Create((CreatedAccount)createdEvent),
                    func: (state, evt) => state.Apply(evt)
                )
            )
        );
    }
}