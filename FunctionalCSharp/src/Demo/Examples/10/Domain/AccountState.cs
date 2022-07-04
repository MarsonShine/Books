namespace Demo.Examples._10.Domain
{
    public enum AccountStatus{ Requested, Active, Frozen, Dormant, Closed }
    public sealed class AccountState
    {
        public AccountState(CurrencyCode currency, AccountStatus status = AccountStatus.Requested, decimal balance = 0, decimal allowedOverdraft = 0)
        {
            Status = status;
            Currency = currency;
            Balance = balance;
            AllowedOverdraft = allowedOverdraft;
        }

        public AccountStatus Status { get; }
        public CurrencyCode Currency { get; }
        public decimal Balance { get; }
        public decimal AllowedOverdraft { get; }
        
        public AccountState Debit(decimal amount) => Credit(-amount);
        // 返回新的对象
        public AccountState Credit(decimal amount) => new AccountState(this.Currency, this.Status, this.Balance + amount, this.AllowedOverdraft);
        // 返回新对象
        public AccountState WithStatus(AccountStatus newStatus) => new AccountState(this.Currency, newStatus, this.Balance, this.AllowedOverdraft);
    }
}