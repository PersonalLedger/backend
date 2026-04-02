using MassTransit;
using Microsoft.Extensions.Logging;
using PersonalLedger.Application.Common.Messaging;

namespace PersonalLedger.Infrastructure.Consumers
{
    // === O CONSUMER DE FALHAS (Monitoramento de _error Queues) ===
    // Ele consome mensagens do tipo Fault<T>. 
    // O MassTransit publica essa mensagem de "Fault" silenciosamente nas filas _error 
    // logo depois que todas as tentativas de Retry acabam e o processo de fato morre.
    public class SyncAccountFaultConsumer : IConsumer<Fault<SyncAccountMessage>>
    {
        private readonly ILogger<SyncAccountFaultConsumer> _logger;

        public SyncAccountFaultConsumer(ILogger<SyncAccountFaultConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<Fault<SyncAccountMessage>> context)
        {
            var falhaDetalhada = context.Message;
            var mensagemOriginal = falhaDetalhada.Message; // Carga (payload) que causou o acidente
            var primeiraException = falhaDetalhada.Exceptions.FirstOrDefault(); // A stacktrace do erro

            // Exemplo prático de Observabilidade Ativa:
            // 1. Gravar em um Log de Erro Crítico Formal 
            //    (Se a empresa usar Datadog, ElasticSearch ou CloudWatch, isso acende um alerta vermelho!)
            _logger.LogCritical(
                "ALERTA GERAL - MENSAGEM MORTA! SyncAccountConsumer falhou permanentemente. ItemId do Pluggy: {ItemId}. Tipo de Erro: {ExceptionType} - Descrição: {ExceptionMessage}",
                mensagemOriginal.PluggyItemId,
                primeiraException?.ExceptionType,
                primeiraException?.Message
            );

            // 2. Em um sistema bancário real corporativo, é neste exato bloco de código que você:
            // -> Dispara um Webhook para um canal do Slack/Teams da equipe de Sustentação ("Erro no sistema x!").
            // -> Envia um e-mail avisando o suporte técnico.
            // -> Atualiza uma tabela de banco de "Transações em Estado de Anomalia" pra um humano auditar depois.

            return Task.CompletedTask;
        }
    }
}
