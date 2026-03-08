namespace FraudEngine.API.Auth;

/// <summary>
/// Constants shared by the API key authentication and authorization flow.
/// </summary>
public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "PartnerApiKey";
    public const string ClientIdHeaderName = "X-Client-Id";
    public const string ApiKeyHeaderName = "X-Api-Key";
    public const string ClientIdClaimType = "client_id";
    public const string ScopeClaimType = "scope";
}
