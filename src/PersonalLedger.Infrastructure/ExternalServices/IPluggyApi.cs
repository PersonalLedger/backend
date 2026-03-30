using Refit;

namespace PersonalLedger.Infrastructure.ExternalServices
{
    internal interface IPluggyApi
    {
        [Get("/accounts")]
        [Headers("Authorization: Bearer")]
        Task<List<PluggyAccountResponse>> GetAccountsAsync();
    }
}
