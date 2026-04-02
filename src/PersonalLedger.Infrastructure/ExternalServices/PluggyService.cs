using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using PersonalLedger.Application.Common.Interfaces;

namespace PersonalLedger.Infrastructure.ExternalServices
{

    public class PluggyService : IPluggyService
    {
        private readonly IPluggyApi _pluggyApi;
        private readonly IDistributedCache _cache;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private const string CacheKey = "PluggyApiKey";

        public PluggyService(IPluggyApi pluggyApi, IDistributedCache cache, IConfiguration configuration)
        {
            _pluggyApi = pluggyApi;
            _cache = cache;
            _clientId = configuration["Pluggy:ClientId"] ?? throw new ArgumentNullException("Pluggy:ClientId was not found in configuration");
            _clientSecret = configuration["Pluggy:ClientSecret"] ?? throw new ArgumentNullException("Pluggy:ClientSecret was not found in configuration");
        }

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private async Task<string> GetApiKeyAsync()
        {
            var cachedKey = await _cache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cachedKey))
            {
                return cachedKey;
            }

            await _semaphore.WaitAsync();
            try
            {
                // Double-check: Quando a thread finalmete entra no cofre, ela verifica se a thread anterior já 
                // não foi lá e buscou o cache pra ela nos últimos milissegundos.
                cachedKey = await _cache.GetStringAsync(CacheKey);
                if (!string.IsNullOrEmpty(cachedKey))
                {
                    return cachedKey;
                }

                // A API Key dura 2 horas. Vamos solicitar uma nova e por no Redis por 110 minutos.
                var response = await _pluggyApi.AuthenticateAsync(new PluggyAuthRequest(_clientId, _clientSecret));
                var newApiKey = response.apiKey;
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(110)
                };
                
                await _cache.SetStringAsync(CacheKey, newApiKey, options);

                return newApiKey;
            }
            finally
            {
                _semaphore.Release(); // Libera a catraca para o próximo
            }
        }

        // Gera o token temporário para a tela do app mobile abrir o login bancário
        // Embute também o Id do usuário no nosso sistema (clientUserId), 
        // para que o Webhook do Pluggy saiba nos devolver quem é o dono dessas transações no futuro!
        public async Task<string> GenerateConnectTokenAsync(string? itemId = null, string? clientUserId = null)
        {
            var apiKey = await GetApiKeyAsync();
            var response = await _pluggyApi.CreateConnectTokenAsync(apiKey, new PluggyConnectTokenRequest(itemId, clientUserId));
            return response.accessToken;
        }

        public async Task<PluggyAccountsPageResponse> GetAccountsAsync(string? itemId = null)
        {
            var apiKey = await GetApiKeyAsync();
            return await _pluggyApi.GetAccountsAsync(apiKey, itemId);
        }

        public async Task<PluggyTransactionsPageResponse> GetTransactionsAsync(string accountId, DateTime? from = null, DateTime? to = null, int page = 1)
        {
            var apiKey = await GetApiKeyAsync();
            
            // O padrão do Pluggy exige a data no formato internacional AAA-MM-DD
            var dataInicio = from?.ToString("yyyy-MM-dd");
            var dataFim = to?.ToString("yyyy-MM-dd");

            return await _pluggyApi.GetTransactionsAsync(apiKey, accountId, dataInicio, dataFim, page);
        }
    }
}
