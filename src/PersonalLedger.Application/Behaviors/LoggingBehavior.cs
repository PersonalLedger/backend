using MediatR;
using Microsoft.Extensions.Logging;

namespace PersonalLedger.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Iniciando requisição: {Name}", requestName);

            var timer = System.Diagnostics.Stopwatch.StartNew();
            var response = await next();
            timer.Stop();

            _logger.LogInformation("Finalizado: {Name} em {Elapsed}ms", requestName, timer.ElapsedMilliseconds);
            return response;
        }
    }
}
