using Refit;

namespace PersonalLedger.Infrastructure.ExternalServices
{
    public interface IPluggyApi
    {
        // 1. Endpoint para gerar a API Key do Backend (Validade de 2 horas)
        // Usada internamente pela API C# para conseguir consultar os dados no Pluggy
        [Post("/auth")]
        Task<PluggyAuthResponse> AuthenticateAsync([Body] PluggyAuthRequest request);

        // 2. Endpoint para gerar o Token do Widget que vai para o App Mobile
        // Por que: O Frontend precisa desse passe-livre de 30 minutos para apresentar a tela de Login bancária.
        [Post("/connect_token")]
        Task<PluggyConnectTokenResponse> CreateConnectTokenAsync([Header("X-API-KEY")] string apiKey, [Body] PluggyConnectTokenRequest request);

        // 3. Consulta de contas usando a API Key mestre
        [Get("/accounts?itemId={itemId}")]
        Task<PluggyAccountsPageResponse> GetAccountsAsync([Header("X-API-KEY")] string apiKey, string? itemId = null);

        // 4. Consulta de Transações de uma conta bancária específica usando a API Key mestre
        // (O Query serve pra filtrarmos de 6 meses atrás até hoje)
        [Get("/transactions?accountId={accountId}")]
        Task<PluggyTransactionsPageResponse> GetTransactionsAsync(
            [Header("X-API-KEY")] string apiKey, 
            string accountId, 
            [Query] string? from = null, 
            [Query] string? to = null, 
            [Query] int page = 1);
    }
}
