using MassTransit;
using Microsoft.Extensions.Logging;
using PersonalLedger.Application.Common.Messaging;
using PersonalLedger.Infrastructure.ExternalServices;
using PersonalLedger.Infrastructure.Messaging.Contracts; // NUNCA ESQUECER DESSA REFERÊNCIA

namespace PersonalLedger.Infrastructure.Consumers
{
    // O Trabalhador (Worker) oficial que processa a sincronização de cada banco logado.
    // Ele fica na Infraestrutura para não sujar a camada de Domínio/Aplicação com dependências do MassTransit.
    public class SyncAccountConsumer : IConsumer<SyncAccountMessage>
    {
        private readonly ILogger<SyncAccountConsumer> _logger;
        private readonly IPluggyService _pluggyService;

        public SyncAccountConsumer(ILogger<SyncAccountConsumer> logger, IPluggyService pluggyService)
        {
            _logger = logger;
            _pluggyService = pluggyService;
        }

        public async Task Consume(ConsumeContext<SyncAccountMessage> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("Worker iniciado! Baixando extratos Pluggy. UserId: {UserId}, ItemId: {ItemId}", message.UserId, message.PluggyItemId);

            try 
            {
                // Regra Histórica de 6 meses
                var hoje = DateTime.UtcNow;
                var seisMesesAtras = hoje.AddMonths(-6);

                // --- PASSO 1: O NOSSO ITEM TEM QUANTAS CONTAS? ---
                // O Cara pode ter logado no Nubank e ter 1 Conta Corrente e 1 Conta PJ no mesmo login.
                var accountsResponse = await _pluggyService.GetAccountsAsync(message.PluggyItemId);
                
                // Vamos percorrer CONTA por CONTA abaixando as transações
                foreach (var account in accountsResponse.Results) 
                {
                    _logger.LogInformation("Lendo a conta: {Name}", account.Name);

                    // --- PASSO 2: PUXANDO O EXTRATO DE UMA CONTA ---
                    int paginaAtual = 1;
                    PluggyTransactionsPageResponse transactionsResponse;
                    
                    do 
                    {
                        // CHAMA A API DO PLUGGY! (Pagina 1, Pagina 2, etc, sempre com as datas)
                        transactionsResponse = await _pluggyService.GetTransactionsAsync(account.Id, seisMesesAtras, hoje, paginaAtual);

                        foreach (var txn in transactionsResponse.Results) 
                        {
                            // Regra que você me pediu das Saidas/Entradas (Lógica pura do seu domínio)
                            bool isEntrada = txn.Amount > 0;
                            var tipoDomain = isEntrada ? "ENTRADA" : "SAÍDA";

                            _logger.LogInformation("Encontrado R${Valor} ({Tipo}) - {Desc}", Math.Abs(txn.Amount), tipoDomain, txn.Description);

                            // (NO FUTURO) O código do seu DbContext virá AQUI DENTRO gravando isso!
                            // _dbContext.Add(new Transaction { Valor = Math.Abs(txn.Amount) });
                        }

                        // Anda pra próxima pagina pois o Pluggy não manda 5 mil registros num array só.
                        paginaAtual++;
                    } 
                    // Se o total de páginas baixadas for menor que o teto da resposta do Pluggy, roda o while de novo.
                    while (paginaAtual <= (transactionsResponse.TotalPages == 0 ? 1 : transactionsResponse.TotalPages));
                }

                _logger.LogInformation("Sincronização assíncrona de 6 meses do Item {ItemId} finalizada com sucesso.", message.PluggyItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento pesado do Item {ItemId}", message.PluggyItemId);
                throw; 
            }
        }
    }

    // === DEFINIÇÃO DE CONFIGURAÇÃO ISOLADA DO CONSUMER ===
    public class SyncAccountConsumerDefinition : ConsumerDefinition<SyncAccountConsumer>
    {
        public SyncAccountConsumerDefinition()
        {
            // Substitui o nome automático por um nome explícito, cravado e seguro! (Passo 4 da IA)
            EndpointName = "critical-sync-pluggy-accounts-queue";
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, 
            IConsumerConfigurator<SyncAccountConsumer> consumerConfigurator, 
            IRegistrationContext context)
        {
            // Configura o Outbox em Memória EXCLUSIVAMENTE para essa Fila Crítica! (Passo 2 da IA)
            endpointConfigurator.UseInMemoryOutbox(context);
        }
    }
}
