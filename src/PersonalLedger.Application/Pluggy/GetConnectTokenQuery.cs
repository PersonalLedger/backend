using MediatR;
using PersonalLedger.Application.Common.Interfaces;

namespace PersonalLedger.Application.Pluggy.Queries
{
    public record GetConnectTokenQuery(string ClientUserId) : IRequest<GetConnectTokenResult>;

    public record GetConnectTokenResult(string AccessToken);

    public class GetConnectTokenHandler : IRequestHandler<GetConnectTokenQuery, GetConnectTokenResult>
    {
        private readonly IPluggyService _pluggyService;

        public GetConnectTokenHandler(IPluggyService pluggyService)
        {
            _pluggyService = pluggyService;
        }

        public async Task<GetConnectTokenResult> Handle(GetConnectTokenQuery request, CancellationToken cancellationToken)
        {
            var token = await _pluggyService.GenerateConnectTokenAsync(clientUserId: request.ClientUserId);
            return new GetConnectTokenResult(token);
        }
    }
}
