using MediatR;

namespace PersonalLedger.Application.Pluggy
{
    public record SyncPluggyItemCommand(Guid UserId, string PluggyItemId) : IRequest;
}
