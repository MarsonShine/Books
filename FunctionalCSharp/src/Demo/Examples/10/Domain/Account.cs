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
            _ => throw new ArgumentException(nameof(evt))
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

        public static (Event Event, AccountState NewState) Debit(this AccountState state, MakeTransfer transfer) {
            Event evt = transfer.ToEvent();
            AccountState newState = state.Apply(evt);   // 计算新状态
            return (evt, newState);
        }

        public static Validation<(Event, AccountState)> Debit(this AccountState account, MakeTransfer transfer) {
            if (account.Status != AccountStatus.Active)
                return Errors.AccountNotActive;
            if (account.Balance - transfer.Amount < account.AllowedOverdraft)
                return Errors.InsufficientBalance; 
        }
    }
}