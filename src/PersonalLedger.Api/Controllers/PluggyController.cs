using MediatR;
using Microsoft.AspNetCore.Mvc;
using PersonalLedger.Application.Pluggy.Queries;

namespace PersonalLedger.Api.Controllers
{

    [ApiController]
    [Route("api/pluggy")]
    public class PluggyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PluggyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Gera um Connect Token descartável (dura 30 min) para o Frontend Mobile abrir o Widget do Pluggy.
        /// </summary>
        /// <remarks>
        /// Por que existe: O Frontend de celular não pode guardar o ClientSecret do servidor por segurança. 
        /// Ele bate nessa rota, nossa API pega a API Key secreta e pede pro Pluggy um passe provisório (accessToken) pro celular.
        /// </remarks>
        [HttpGet("token")]
        public async Task<IActionResult> GetConnectToken(CancellationToken cancellationToken)
        {
            try
            {
                var clientUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(clientUserId))
                    return Unauthorized();

                var result = await _mediator.Send(new GetConnectTokenQuery(clientUserId), cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Falha ao gerar o token de conexão do Pluggy", details = ex.Message });
            }
        }
    }
}
