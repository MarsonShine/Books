using Demo.Examples._10.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Examples._10
{
    public class MakeTransferController: Controller
    {
        Func<Guid, AccountState> getAccount;
        Action<Event> saveAndPublish;

        public IActionResult MakeTransfer([FromBody] MakeTransfer cmd) {
            var account = getAccount(cmd.DebitedAccountId);
            // performs the transfer
            var (evt, newState) = account.Debit(cmd);
            saveAndPublish(evt);
            // returns information to the user about the new state
            return Ok(new { Balance = newState.Balance });
        }
    }

    public class MakeTransfer {
        public Guid DebitedAccountId { get; }
        public string Beneficiary { get; internal set; }
        public string Bic { get; internal set; }
        public decimal Amount { get; internal set; }
        public string Iban { get; internal set; }
        public string Reference { get; internal set; }
        public DateTime Timestamp { get; internal set; }
    }
}