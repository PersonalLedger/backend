namespace PersonalLedger.Infrastructure.ExternalServices
{
    // Auth - Autenticação da nossa API C# (Backend) no servidor do Pluggy
    // Por que: Para buscar contas e criar tokens do widget, usamos a apiKey retornada aqui (dura 2h).
    public record PluggyAuthRequest(string client_id, string client_secret);
    public record PluggyAuthResponse(string apiKey);

    // Connect Token - Token pro Front-End (App Mobile) abrir a "Telinha do Banco"
    // Por que: Por segurança, o celular nunca sabe nosso client_secret. Ele recebe só esse token
    // temporário (dura 30 min) para poder exibir o Widget do banco pro usuário de forma segura.
    public record PluggyConnectTokenRequest(string? itemId = null, string? clientUserId = null);
    public record PluggyConnectTokenResponse(string accessToken);

    // Contas - Representam a Conta Bancária Conectada
    // Por que: Traz os dados consolidados do saldo de um item (ex: Conta Corrente Itaú).
    public record PluggyAccountResponse(string Id, string Name, decimal Balance, string Currency, string Type, string Subtype);
    public record PluggyAccountsPageResponse(int Total, int TotalPages, int Page, List<PluggyAccountResponse> Results);

    // Transacoes - Cada linha do extrato bancário
    public record PluggyTransactionResponse(string Id, string Description, decimal Amount, DateTime Date, string Currency, string Status);
    public record PluggyTransactionsPageResponse(int Total, int TotalPages, int Page, List<PluggyTransactionResponse> Results);

    // Webhooks - Eventos enviados do Pluggy quando chega Pix, compra no cartão, etc.
    public record PluggyWebhookPayload(string Event, string ItemId, string WebhookId, string ClientUserId);
}
