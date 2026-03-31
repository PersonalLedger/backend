using Microsoft.AspNetCore.Mvc;
using PersonalLedger.Infrastructure.ExternalServices;

namespace PersonalLedger.Api.Controllers
{
    // Arquitetura: Controller dedicado à integração primária do App Mobile com o Pluggy
    // Por que: Aqui isolamos as rotas que o Frontend (celular) consome diretamente.
    [ApiController]
    [Route("api/[controller]")]
    public class PluggyController : ControllerBase
    {
        private readonly IPluggyService _pluggyService;

        public PluggyController(IPluggyService pluggyService)
        {
            _pluggyService = pluggyService;
        }

        /// <summary>
        /// Gera um Connect Token descartável (dura 30 min) para o Frontend Mobile abrir o Widget do Pluggy.
        /// </summary>
        /// <remarks>
        /// Por que existe: O Frontend de celular não pode guardar o ClientSecret do servidor por segurança. 
        /// Ele bate nessa rota, nossa API pega a API Key secreta e pede pro Pluggy um passe provisório (accessToken) pro celular.
        /// </remarks>
        [HttpGet("token")]
        public async Task<IActionResult> GetConnectToken([FromQuery] string clientUserId)
        {
            try
            {
                // Chamamos o serviço orquestrador passando o "ClientUserId" (o ID do usuário do nosso banco).
                // Isso amarra a conexão aos Webhooks futuros do Pluggy.
                var token = await _pluggyService.GenerateConnectTokenAsync(clientUserId: clientUserId);
                
                return Ok(new { accessToken = token });
            }
            catch (Exception ex)
            {
                // Todo: Implementar um Logger ou Middleware global de Exceptions seguindo o padrão da sua aplicação.
                return StatusCode(500, new { message = "Falha ao gerar o token de conexão do Pluggy", details = ex.Message });
            }
        }
    }
}
