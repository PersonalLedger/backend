using Microsoft.AspNetCore.Mvc;
using PersonalLedger.Infrastructure.ExternalServices;
using MassTransit;
using PersonalLedger.Application.Common.Messaging; // Você já deixou configurado brilhantemente no Infrastructure!

namespace PersonalLedger.Api.Controllers
{
    // Criei um record rápido para a mensagem da fila. Depois você pode mover para a camada de Application/Contracts.
    public record SyncTransactionsCommand(string ItemId, string EventType);

    // Arquitetura: Controller que recebe os "Toques/Avisos" diretos do servidor do Pluggy.
    [ApiController]
    [Route("api/webhooks/pluggy")]
    public class PluggyWebhookController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public PluggyWebhookController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// Rota pública e extremamente rápida para receber as atualizações em tempo real.
        /// </summary>
        /// <remarks>
        /// Por que esse código é pequeno? Porque a resposta HTTP 200 pro Pluggy tem que ser em milissegundos.
        /// O trabalho pesado é delegado para o RabbitMQ na fila.
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook([FromBody] PluggyWebhookPayload payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.ItemId))
            {
                return BadRequest(new { message = "Payload inválido ou ItemId ausente." });
            }

            // O Pluggy envia o UserId do nosso cliente via clientUserId, que mandamos durante o ConnectToken
            if (Guid.TryParse(payload.ClientUserId, out Guid userId))
            {
                // Disparamos o Consumer nativo do projeto (SyncAccountMessage que está em Common/Messaging)
                await _publishEndpoint.Publish(new SyncAccountMessage(userId, payload.ItemId));
            }
            else
            {
                // Se a conversão falhar ou o clientUserId vier nulo, podemos logar a falta do Id ou tratar.
            }

            // Retornamos velozmente o 200 pro Pluggy não gerar Timeout.
            return Ok();
        }
    }
}
