using Microsoft.AspNetCore.Mvc;
using MarsonShine.Functional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Examples._5
{
    using static MarsonShine.Functional.F;
    public class MakeTransferController : Controller
    {
        readonly IValidator<MakeTransfer> validator;
        public MakeTransferController(IValidator<MakeTransfer> validator)
        {
            this.validator = validator;
        }

        [HttpPost("api/makeTransfer")]
        public void MakeTransfer([FromBody] MakeTransfer transfer)
        {
            if (validator!.IsValid(transfer)) // 使用函数组合来代替if结构
                Book(transfer);
        }

        private void Book(MakeTransfer transfer)
        {
            // ...
        }

        public void MakeTransfer2([FromBody] MakeTransfer transfer) => Some(transfer)
            .Where(validator.IsValid)
            .ForEach(Book);

    }

    public class MakeTransfer
    {

    }

    // 函数式改造，控制与逻辑分离
    // 该类只包含数据
    public class AccountState
    {
        public decimal Balance { get; }
        public AccountState(decimal balance)
        {
            Balance = balance;
        }
    }
    // 只包含逻辑
    public static class Account
    {
        public static Option<AccountState> Debit(this AccountState acc, decimal amount) => acc.Balance < amount ? None : Some(new AccountState(acc.Balance - amount));
    }

    // 比较一下OOM下的代码风格
    public class AccountOOP
    {
        public decimal Balance { get; private set; }
        public AccountOOP(decimal balance)
        {
            Balance = balance;
        }
        // 上述函数式Debit是可以进行逻辑组合的，OOP风格下的不行，不具有重用性
        public void Debit(decimal amount)
        {
            if (Balance < amount)
                throw new InvalidOperationException("Insufficient funds");
            Balance -= amount;
        }
    }
}
