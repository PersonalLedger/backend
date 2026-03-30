namespace PersonalLedger.Application.Common.Messaging
{
    public record SyncAccountMessage(Guid UserId, string PluggyItemId);
}
