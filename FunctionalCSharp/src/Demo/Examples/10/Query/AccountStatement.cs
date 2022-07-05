using MarsonShine.Functional;

namespace Demo.Examples._10.Query
{
    using static F;
    public class Transaction
    {
        public Transaction(DebitedTransfer e)
        {
            DebitedAmount = e.DebitedAmount;
            Description = $"Transfer to {e.Bic}/{e.Iban}; Ref: {e.Reference}";
            Date = e.Timestamp.Date;
        }
        public Transaction(DepositedCash e)
        {
            CreditedAmount = e.Amount;
            Description = $"Deposit at {e.BranchId}";
            Date = e.Timestamp.Date;
        }

        public DateTime Date { get; }
        public decimal DebitedAmount { get; }
        public decimal CreditedAmount { get; }
        public string Description { get; }
    }
    // 根据历史事件来构建状态视图
    public class AccountStatement
    {
        public AccountStatement(int month, int year, IEnumerable<Event> events)
        {
            var startOfPeriod = new DateTime(year, month, 1);
            var endOfPeriod = startOfPeriod.AddMonths(1);

            var eventsBeforePeriod = events.TakeWhile(e => e.Timestamp < startOfPeriod);
            var eventsInPeriod = events.SkipWhile(e => e.Timestamp < startOfPeriod)
                .TakeWhile(e => endOfPeriod < e.Timestamp);

            StartingBalance = eventsBeforePeriod.Aggregate(0m, BalanceReducer);
            EndBalance = eventsInPeriod.Aggregate(StartingBalance, BalanceReducer);

            Transactions = eventsInPeriod.Bind(CreatedTransaction);
        }

        Option<Transaction> CreatedTransaction(Event evt) => evt switch
        {
            DepositedCash e => new Transaction(e),
            DebitedTransfer e => new Transaction(e),
            _ => None
        };

        decimal BalanceReducer(decimal bal, Event evt) => evt switch
        {
            DepositedCash e => bal + e.Amount,
            DebitedTransfer e => bal - e.DebitedAmount,
            _ => bal
        };

        public int Month { get; }
        public int Year { get; }
        public decimal StartingBalance { get; }
        public decimal EndBalance { get; }
        public IEnumerable<Transaction> Transactions { get; }


    }
}
