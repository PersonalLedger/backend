using MediatR;
using Microsoft.Extensions.Logging;
using PersonalLedger.Application.Common.Interfaces;
using PersonalLedger.Infrastructure.ExternalServices;

namespace PersonalLedger.Application.Pluggy
{
    public class SyncPluggyItemCommandHandler : IRequestHandler<SyncPluggyItemCommand>
    {
        private readonly ILogger<SyncPluggyItemCommandHandler> _logger;
        private readonly IPluggyService _pluggyService;

        public SyncPluggyItemCommandHandler(ILogger<SyncPluggyItemCommandHandler> logger, IPluggyService pluggyService)
        {
            _logger = logger;
            _pluggyService = pluggyService;
        }

        public async Task Handle(SyncPluggyItemCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando processamento das transações da Pluggy via Caso de Uso. UserId: {UserId}, ItemId: {ItemId}", request.UserId, request.PluggyItemId);

            try
            {
                // Regra Histórica de 6 meses
                var hoje = DateTime.UtcNow;
                var seisMesesAtras = hoje.AddMonths(-6);

                // --- PASSO 1: O NOSSO ITEM TEM QUANTAS CONTAS? ---
                var accountsResponse = await _pluggyService.GetAccountsAsync(request.PluggyItemId);

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

                        // (NO FUTURO) O código do seu Repositório/Serviço de Domínio virá AQUI DENTRO gravando isso!
                        // Exemplo: _transactionRepository.AddAsync(new Transaction(....));
                    }

                    paginaAtual++;
                }
                while (paginaAtual <= (transactionsResponse.TotalPages == 0 ? 1 : transactionsResponse.TotalPages));
                }

                _logger.LogInformation("Sincronização assíncrona de 6 meses do Item {ItemId} finalizada com sucesso pelo Handler.", request.PluggyItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na camada Application no processamento do Item {ItemId}", request.PluggyItemId);
                throw;
            }
        }
    }
}
