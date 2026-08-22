namespace HomeDiary_api.Security;

/// <summary>
/// Request-scoped tenancy context. The value is established by the validated
/// Auth0 subject's HomeDiary user record and is never read from request input.
/// </summary>
public sealed class ClientContext
{
    public int? ClientId { get; private set; }
    public int? UserId { get; private set; }

    public void Set(int clientId, int userId)
    {
        if (ClientId is not null && ClientId != clientId)
            throw new InvalidOperationException("The client context cannot be changed during a request.");
        ClientId = clientId;
        UserId = userId;
    }

    public int RequireClientId() => ClientId
        ?? throw new InvalidOperationException("The authenticated user has not completed client onboarding.");
}
