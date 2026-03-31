using PersonalLedger.Application.Common.Constants;

namespace PersonalLedger.Application.Common.Messaging
{
    public abstract class PluggyConsumerDefinition<T> : ConsumerDefinition<T> where T : class, IConsumer
    {
        protected PluggyConsumerDefinition()
        {
            // Define o nome da fila usando a constante
            EndpointName = RabbitQueues.PluggyIntegration;

            // Configuração de escala: quantas mensagens processar em paralelo
            ConcurrentMessageLimit = 10;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<T> consumerConfigurator, IRegistrationContext context)
        {
            // Se a API do Pluggy der erro, ele tenta 3 vezes antes de mandar para a fila de erro
            endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        }
    }
}
