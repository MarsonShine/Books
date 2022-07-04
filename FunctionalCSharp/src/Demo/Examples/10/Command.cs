namespace Demo.Examples._10
{
    public class Command
    {
        
    }

    public static class CommandExt {
        public static DebitedTransfer ToEvent(this MakeTransfer cmd) => new DebitedTransfer {
            Beneficiary = cmd.Beneficiary,
            Bic = cmd.Bic,
            DebitedAmount = cmd.Amount,
            EntityId = cmd.DebitedAccountId,
            Iban = cmd.Iban,
            Reference = cmd.Reference,
            Timestamp = cmd.Timestamp
        };
    }
}