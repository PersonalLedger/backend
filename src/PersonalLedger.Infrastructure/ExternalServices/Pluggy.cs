namespace PersonalLedger.Infrastructure.ExternalServices
{
    public record PluggyAccountResponse(string Id, string Name, decimal Balance, string Currency);
    public record PluggyTransactionResponse(string Id, string Description, decimal Amount, DateTime Date);
}
