using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using PersonalLedger.Application.Common.Messaging;
using PersonalLedger.Application.Pluggy;
using PersonalLedger.Infrastructure.Messaging.Contracts;

namespace PersonalLedger.Infrastructure.Consumers
{
    // O Trabalhador (Worker) oficial que processa a sincronização de cada banco logado.
    // Ele fica na Infraestrutura e atua livre de regras de negócio, apenas enviando os dados para a Aplicação (Clean Architecture puro).
    public class SyncAccountConsumer : IConsumer<SyncAccountMessage>
    {
        private readonly ILogger<SyncAccountConsumer> _logger;
        private readonly ISender _sender;

        public SyncAccountConsumer(ILogger<SyncAccountConsumer> logger, ISender sender)
        {
            _logger = logger;
            _sender = sender;
        }

        public async Task Consume(ConsumeContext<SyncAccountMessage> context)
        {
            var message = context.Message;
            
            _logger.LogInformation("Worker (MassTransit) recebeu a mensagem! Delegando processamento para a camada de Application. UserId: {UserId}, ItemId: {ItemId}", message.UserId, message.PluggyItemId);

            try 
            {
                // Devolvemos o comando de Sincronização para ser tratado no "coração" do sistema (Application)
                await _sender.Send(new SyncPluggyItemCommand(message.UserId, message.PluggyItemId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no disparador do Worker para o Item {ItemId}", message.PluggyItemId);
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
