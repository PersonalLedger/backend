using PersonalLedger.Infrastructure.ExternalServices;

namespace PersonalLedger.Application.Common.Interfaces
{
    public interface IPluggyService
    {
        Task<string> GenerateConnectTokenAsync(string? itemId = null, string? clientUserId = null);
        Task<PluggyAccountsPageResponse> GetAccountsAsync(string? itemId = null);
        Task<PluggyTransactionsPageResponse> GetTransactionsAsync(string accountId, DateTime? from = null, DateTime? to = null, int page = 1);
    }
}
