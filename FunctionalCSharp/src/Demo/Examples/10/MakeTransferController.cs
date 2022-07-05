using Demo.Examples._10.Domain;
using MarsonShine.Functional;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Examples._10
{
    public class MakeTransferController : Controller
    {
        Func<MakeTransfer, Validation<MakeTransfer>> validate;
        Func<Guid, AccountState> getAccount;
        Action<Event> saveAndPublish;

        public IActionResult MakeTransfer([FromBody] MakeTransfer cmd)
        {
            var account = getAccount(cmd.DebitedAccountId);
            // performs the transfer
            var (evt, newState) = account.Debit(cmd);
            saveAndPublish(evt);
            // returns information to the user about the new state
            return Ok(new { Balance = newState.Balance });
        }
        // 添加状态验证之后的第二版本
        public IActionResult MakeTransfer2([FromBody] MakeTransfer cmd) =>
            validate(cmd)
                .Bind(t => getAccount(t.DebitedAccountId).DebitWithValidation(t))
                .Do(tuple => saveAndPublish(tuple.Item1))
                .Match<IActionResult>(
                Invalid: errs => BadRequest(new { Errors = errs }),
                Valid: result => Ok(new { Balance = result.Item2.Balance })
                );
    }

    public static class Account
    {
        public static (Event Event, AccountState NewState) Debit
            (this AccountState @this, MakeTransfer transfer)
        {
            var evt = transfer.ToEvent();
            var newState = @this.Apply(evt);

            return (evt, newState);
        }

    }

    public class MakeTransfer
    {
        public Guid DebitedAccountId { get; }
        public string Beneficiary { get; internal set; }
        public string Bic { get; internal set; }
        public decimal Amount { get; internal set; }
        public string Iban { get; internal set; }
        public string Reference { get; internal set; }
        public DateTime Timestamp { get; internal set; }
    }
}